using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;
using SolicitudIT_Isertec.Models;
using System.Text;

namespace SolicitudIT_Isertec.Controllers;

[Authorize(Roles = "admin")]
public class AdminController : Controller
{
    private readonly AppDbContext db;

    public AdminController(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IActionResult> Index()
    {
        return View(await BuildVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserSave(
        int? id,
        string nombre,
        string apellido,
        string usuario,
        string email,
        string? password,
        Rol rol,
        bool activo = true)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(apellido) ||
            string.IsNullOrWhiteSpace(usuario) ||
            string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Completa los datos obligatorios del usuario.";
            return RedirectToAction(nameof(Index));
        }

        var loginExiste = await db.Usuarios.AnyAsync(x =>
            x.UsuarioLogin == usuario.Trim() &&
            (!id.HasValue || x.Id != id.Value));

        var emailExiste = await db.Usuarios.AnyAsync(x =>
            x.Email == email.Trim() &&
            (!id.HasValue || x.Id != id.Value));

        if (loginExiste || emailExiste)
        {
            TempData["Error"] = loginExiste
                ? "Ese usuario ya existe."
                : "Ese correo ya está registrado.";
            return RedirectToAction(nameof(Index));
        }

        Usuario? u;

        if (id.HasValue)
        {
            u = await db.Usuarios.FindAsync(id.Value);
            if (u == null)
                return NotFound();
        }
        else
        {
            u = new Usuario();
            db.Usuarios.Add(u);
        }

        u.Nombre = nombre.Trim();
        u.Apellido = apellido.Trim();
        u.UsuarioLogin = usuario.Trim();
        u.Email = email.Trim();
        u.Rol = rol;
        u.Activo = activo;
        u.ActualizadoEn = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(password))
        {
            u.Password = BCrypt.Net.BCrypt.HashPassword(password);
        }
        else if (!id.HasValue)
        {
            TempData["Error"] = "La contraseña es obligatoria al crear un usuario.";
            return RedirectToAction(nameof(Index));
        }

        await db.SaveChangesAsync();

        TempData["Success"] = id.HasValue
            ? "Usuario actualizado correctamente."
            : "Usuario creado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserDelete(int id)
    {
        if (id == GetCurrentUserId())
        {
            TempData["Error"] = "No puedes eliminar la cuenta de administración con la que estás conectado.";
            return RedirectToAction(nameof(Index));
        }

        var user = await db.Usuarios.FindAsync(id);
        if (user == null)
            return NotFound();

        var tieneSolicitudesCreadas = await db.Solicitudes.AnyAsync(x => x.UsuarioId == id);
        var tieneSolicitudesAsignadas = await db.Solicitudes.AnyAsync(x => x.TecnicoId == id);
        var tieneSolicitudesSolicitadas = await db.Solicitudes.AnyAsync(x => x.TecnicoSolicitadoId == id);
        var tieneComentarios = await db.Comentarios.AnyAsync(x => x.UsuarioId == id);
        var tieneAdjuntos = await db.Adjuntos.AnyAsync(x => x.UsuarioId == id);
        var tieneHistorial = await db.HistorialSolicitudes.AnyAsync(x => x.UsuarioId == id);

        var tieneHistorialRelacionado =
            tieneSolicitudesCreadas ||
            tieneSolicitudesAsignadas ||
            tieneSolicitudesSolicitadas ||
            tieneComentarios ||
            tieneAdjuntos ||
            tieneHistorial;

        var equipos = await db.Equipos
            .Where(x => x.UsuarioId == id)
            .ToListAsync();

        foreach (var equipo in equipos)
            equipo.UsuarioId = null;

        // Si el usuario tiene historial, no se borra físicamente porque
        // las solicitudes conservan su relación con el creador. En su lugar
        // se elimina su acceso y se anonimiza la cuenta, conservando el
        // historial de soporte.
        if (tieneHistorialRelacionado)
        {
            user.Nombre = "Usuario";
            user.Apellido = "eliminado";
            user.UsuarioLogin = $"eliminado_{id}_{Guid.NewGuid():N}";
            user.Email = $"eliminado_{id}@local.invalid";
            user.Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));
            user.Rol = Rol.usuario;
            user.Activo = false;
            user.ActualizadoEn = DateTime.UtcNow;

            // Se conserva la relación con las solicitudes para no perder
            // el historial de quién fue responsable. Los datos personales
            // del usuario ya quedaron anonimizados.
            await db.SaveChangesAsync();

            TempData["Success"] = "Usuario eliminado y anonimizado correctamente. Se conservó el historial de solicitudes.";
            return RedirectToAction(nameof(Index));
        }

        db.Usuarios.Remove(user);
        await db.SaveChangesAsync();

        TempData["Success"] = "Usuario eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EquipmentSave(
        int? id,
        string codigo,
        string tipo,
        string? marca,
        string? modelo,
        string? numeroSerie,
        int? usuarioId,
        string? ubicacion,
        EstadoEquipo estado,
        string? observaciones)
    {
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(tipo))
        {
            TempData["Error"] = "El código y el tipo del equipo son obligatorios.";
            return RedirectToAction(nameof(Index));
        }

        var codigoExiste = await db.Equipos.AnyAsync(x =>
            x.Codigo == codigo.Trim() &&
            (!id.HasValue || x.Id != id.Value));

        if (codigoExiste)
        {
            TempData["Error"] = "Ya existe un equipo con ese código.";
            return RedirectToAction(nameof(Index));
        }

        if (usuarioId.HasValue)
        {
            var usuarioValido = await db.Usuarios.AnyAsync(x =>
                x.Id == usuarioId.Value && x.Activo);

            if (!usuarioValido)
            {
                TempData["Error"] = "El usuario seleccionado no es válido.";
                return RedirectToAction(nameof(Index));
            }
        }

        Equipo? e;

        if (id.HasValue)
        {
            e = await db.Equipos.FindAsync(id.Value);
            if (e == null)
                return NotFound();
        }
        else
        {
            e = new Equipo();
            db.Equipos.Add(e);
        }

        e.Codigo = codigo.Trim();
        e.Tipo = tipo.Trim();
        e.Marca = string.IsNullOrWhiteSpace(marca) ? null : marca.Trim();
        e.Modelo = string.IsNullOrWhiteSpace(modelo) ? null : modelo.Trim();
        e.NumeroSerie = string.IsNullOrWhiteSpace(numeroSerie) ? null : numeroSerie.Trim();
        e.UsuarioId = usuarioId;
        e.Ubicacion = string.IsNullOrWhiteSpace(ubicacion) ? null : ubicacion.Trim();
        e.Estado = estado;
        e.Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        e.ActualizadoEn = DateTime.UtcNow;

        await db.SaveChangesAsync();

        TempData["Success"] = id.HasValue
            ? "Equipo actualizado correctamente."
            : "Equipo registrado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EquipmentDelete(int id)
    {
        var equipo = await db.Equipos.FindAsync(id);
        if (equipo == null)
            return NotFound();

        var estaEnSolicitudes = await db.Solicitudes.AnyAsync(x => x.EquipoId == id);

        if (estaEnSolicitudes)
        {
            TempData["Error"] = "No se puede eliminar este equipo porque está relacionado con una solicitud. Puedes cambiar su estado a baja desde Editar equipo.";
            return RedirectToAction(nameof(Index));
        }

        db.Equipos.Remove(equipo);
        await db.SaveChangesAsync();

        TempData["Success"] = "Equipo eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportRequestsPdf()
    {
        var solicitudes = await db.Solicitudes
            .Include(x => x.Usuario)
            .Include(x => x.Categoria)
            .Include(x => x.Equipo)
            .Include(x => x.Tecnico)
            .OrderByDescending(x => x.CreadoEn)
            .ToListAsync();

        var lines = new List<string>
        {
            "REPORTE DE SOLICITUDES - SOPORTE BT",
            $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}",
            "",
            "Codigo | Solicitante | Categoria | Equipo | AnyDesk | Prioridad | Estado | Responsable | Creado"
        };

        foreach (var x in solicitudes)
        {
            var solicitante = $"{x.Usuario?.Nombre} {x.Usuario?.Apellido}".Trim();
            var equipo = x.Equipo == null ? "Sin dispositivo" : x.Equipo.Codigo;
            var tecnico = x.Tecnico == null ? "Sin asignar" : $"{x.Tecnico.Nombre} {x.Tecnico.Apellido}".Trim();
            lines.Add(
                $"{x.Codigo} | {solicitante} | {x.Categoria?.Nombre} | {equipo} | {x.AnyDeskId} | {x.Prioridad} | {x.Estado} | {tecnico} | {x.CreadoEn.ToLocalTime():dd/MM/yyyy HH:mm}"
            );
        }

        var pdf = BuildSimplePdf(lines);
        return File(pdf, "application/pdf", $"reporte-solicitudes-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }

    private static byte[] BuildSimplePdf(IEnumerable<string> sourceLines)
    {
        // PDF sencillo y autónomo para no depender de una API externa.
        // Se usa Helvetica/WinAnsi y se normalizan caracteres fuera de ASCII.
        var lines = sourceLines
            .Select(x => NormalizePdfText(x))
            .ToList();

        const int linesPerPage = 48;
        var pages = new List<List<string>>();
        for (var i = 0; i < lines.Count; i += linesPerPage)
            pages.Add(lines.Skip(i).Take(linesPerPage).ToList());
        if (pages.Count == 0)
            pages.Add(new List<string> { "Sin registros." });

        var objects = new List<string>();
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add(""); // Pages, filled below.
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");

        var pageObjectNumbers = new List<int>();
        var contentObjectNumbers = new List<int>();

        foreach (var page in pages)
        {
            var content = new StringBuilder();
            content.AppendLine("BT");
            content.AppendLine("/F1 9 Tf");
            content.AppendLine("36 806 Td");
            content.AppendLine("12 TL");

            for (var i = 0; i < page.Count; i++)
            {
                var size = i == 0 ? 13 : 9;
                if (i == 0)
                    content.AppendLine("/F1 13 Tf");
                else if (i == 1)
                    content.AppendLine("/F1 8 Tf");
                else if (i == 3)
                    content.AppendLine("/F1 8 Tf");

                content.AppendLine($"({EscapePdfText(page[i])}) Tj");
                content.AppendLine("0 -12 Td");
            }

            content.AppendLine("ET");
            var contentObj = objects.Count + 1;
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}endstream");
            contentObjectNumbers.Add(contentObj);

            var pageObj = objects.Count + 1;
            objects.Add("");
            pageObjectNumbers.Add(pageObj);
        }

        var kids = string.Join(" ", pageObjectNumbers.Select(n => $"{n} 0 R"));
        objects[1] = $"<< /Type /Pages /Kids [{kids}] /Count {pageObjectNumbers.Count} >>";

        for (var i = 0; i < pageObjectNumbers.Count; i++)
        {
            var pageObjIndex = pageObjectNumbers[i] - 1;
            objects[pageObjIndex] =
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumbers[i]} 0 R >>";
        }

        using var ms = new MemoryStream();
        void Write(string s) => ms.Write(Encoding.ASCII.GetBytes(s));

        Write("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            Write($"{i + 1} 0 obj\n");
            Write(objects[i]);
            Write("\nendobj\n");
        }

        var xref = ms.Position;
        Write($"xref\n0 {objects.Count + 1}\n");
        Write("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
            Write($"{offsets[i]:D10} 00000 n \n");

        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return ms.ToArray();
    }

    private static string EscapePdfText(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string NormalizePdfText(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(c <= 127 ? c : '?');
        }
        return sb.ToString();
    }

    [HttpGet]
    public async Task<IActionResult> ExportRequestsCsv()
    {
        var solicitudes = await db.Solicitudes
            .Include(x => x.Usuario)
            .Include(x => x.Categoria)
            .Include(x => x.Equipo)
            .Include(x => x.Tecnico)
            .OrderByDescending(x => x.CreadoEn)
            .ToListAsync();

        static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", new[] { "Código", "Solicitante", "Correo", "Categoría", "Equipo", "AnyDesk", "Prioridad", "Estado", "Técnico", "Creado", "Resuelto" }.Select(Csv)));

        foreach (var x in solicitudes)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                x.Codigo,
                $"{x.Usuario?.Nombre} {x.Usuario?.Apellido}",
                x.Usuario?.Email,
                x.Categoria?.Nombre,
                x.Equipo?.Codigo,
                x.AnyDeskId,
                x.Prioridad.ToString(),
                x.Estado.ToString(),
                $"{x.Tecnico?.Nombre} {x.Tecnico?.Apellido}",
                x.CreadoEn.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                x.ResueltoEn?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            }.Select(Csv)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"reporte-solicitudes-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MaintenanceSave(int? id, string codigo, string nombre, string? descripcion, string? tipo, int stockActual, int stockMinimo, string? ubicacion, bool activo = true)
    {
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || stockActual < 0 || stockMinimo < 0)
        {
            TempData["Error"] = "Completa código, nombre y cantidades válidas para el inventario.";
            return RedirectToAction(nameof(Index));
        }

        var codigoExiste = await db.InventarioMantenimiento.AnyAsync(x => x.Codigo == codigo.Trim() && (!id.HasValue || x.Id != id.Value));
        if (codigoExiste)
        {
            TempData["Error"] = "Ya existe un artículo con ese código.";
            return RedirectToAction(nameof(Index));
        }

        InventarioMantenimiento item;
        if (id.HasValue)
        {
            item = await db.InventarioMantenimiento.FindAsync(id.Value);
            if (item == null) return NotFound();
        }
        else
        {
            item = new InventarioMantenimiento();
            db.InventarioMantenimiento.Add(item);
        }

        item.Codigo = codigo.Trim();
        item.Nombre = nombre.Trim();
        item.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        item.Tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim();
        item.StockActual = stockActual;
        item.StockMinimo = stockMinimo;
        item.Ubicacion = string.IsNullOrWhiteSpace(ubicacion) ? null : ubicacion.Trim();
        item.Activo = activo;
        item.ActualizadoEn = DateTime.UtcNow;

        await db.SaveChangesAsync();
        TempData["Success"] = id.HasValue ? "Artículo de mantenimiento actualizado." : "Artículo de mantenimiento registrado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MaintenanceDelete(int id)
    {
        var item = await db.InventarioMantenimiento.FindAsync(id);
        if (item == null) return NotFound();
        db.InventarioMantenimiento.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Artículo de mantenimiento eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminVm> BuildVm()
    {
        return new AdminVm
        {
            Usuarios = await db.Usuarios
                .Where(x => !x.UsuarioLogin.StartsWith("eliminado_"))
                .OrderBy(x => x.Nombre)
                .ThenBy(x => x.Apellido)
                .ToListAsync(),

            Equipos = await db.Equipos
                .Include(x => x.Usuario)
                .OrderBy(x => x.Codigo)
                .ToListAsync(),

            UsuariosActivos = await db.Usuarios
                .Where(x => x.Activo)
                .OrderBy(x => x.Nombre)
                .ThenBy(x => x.Apellido)
                .ToListAsync(),

            Categorias = await db.Categorias
                .OrderBy(x => x.Nombre)
                .ToListAsync(),

            InventarioMantenimiento = await db.InventarioMantenimiento
                .OrderBy(x => x.Nombre)
                .ToListAsync(),

            TotalSolicitudes = await db.Solicitudes.CountAsync(),
            SolicitudesActivas = await db.Solicitudes.CountAsync(x => x.Estado != EstadoSolicitud.resuelta && x.Estado != EstadoSolicitud.cerrada && x.Estado != EstadoSolicitud.cancelada),
            SolicitudesResueltas = await db.Solicitudes.CountAsync(x => x.Estado == EstadoSolicitud.resuelta || x.Estado == EstadoSolicitud.cerrada),
            SolicitudesCanceladas = await db.Solicitudes.CountAsync(x => x.Estado == EstadoSolicitud.cancelada),
            SolicitudesEsteMes = await db.Solicitudes.CountAsync(x => x.CreadoEn >= DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day) && x.CreadoEn < DateTime.UtcNow.Date.AddMonths(1).AddDays(1 - DateTime.UtcNow.Day)),
            SolicitudesPorTecnico = await db.Solicitudes
                .Where(x => x.TecnicoId.HasValue)
                .GroupBy(x => new { x.TecnicoId, Nombre = x.Tecnico!.Nombre, Apellido = x.Tecnico.Apellido })
                .Select(g => new DashboardTecnicoVm { Nombre = g.Key.Nombre + " " + g.Key.Apellido, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.Nombre)
                .ToListAsync(),
            SolicitudesPorEstado = await db.Solicitudes
                .GroupBy(x => x.Estado)
                .Select(g => new DashboardEstadoVm { Estado = g.Key.ToString(), Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .ToListAsync(),
            SolicitudesPorCategoria = await db.Solicitudes
                .Include(x => x.Categoria)
                .GroupBy(x => x.Categoria!.Nombre)
                .Select(g => new DashboardCategoriaVm { Categoria = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(8)
                .ToListAsync()
        };
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }
}

public class AdminVm
{
    public List<Usuario> Usuarios { get; set; } = new();
    public List<Equipo> Equipos { get; set; } = new();
    public List<Usuario> UsuariosActivos { get; set; } = new();
    public List<Categoria> Categorias { get; set; } = new();
    public List<InventarioMantenimiento> InventarioMantenimiento { get; set; } = new();
    public int TotalSolicitudes { get; set; }
    public int SolicitudesActivas { get; set; }
    public int SolicitudesResueltas { get; set; }
    public int SolicitudesCanceladas { get; set; }
    public int SolicitudesEsteMes { get; set; }
    public List<DashboardTecnicoVm> SolicitudesPorTecnico { get; set; } = new();
    public List<DashboardEstadoVm> SolicitudesPorEstado { get; set; } = new();
    public List<DashboardCategoriaVm> SolicitudesPorCategoria { get; set; } = new();
}

public class DashboardTecnicoVm
{
    public string Nombre { get; set; } = "";
    public int Total { get; set; }
}

public class DashboardEstadoVm
{
    public string Estado { get; set; } = "";
    public int Total { get; set; }
}

public class DashboardCategoriaVm
{
    public string Categoria { get; set; } = "";
    public int Total { get; set; }
}
