using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;
using SolicitudIT_Isertec.Models;
using SolicitudIT_Isertec.Services;
using System.ComponentModel.DataAnnotations;

namespace SolicitudIT_Isertec.Controllers;

[Authorize]
public class SolicitudesController : Controller
{
    private readonly AppDbContext db;
    private readonly NotificationService notify;
    private readonly IWebHostEnvironment env;

    public SolicitudesController(
        AppDbContext db,
        NotificationService notify,
        IWebHostEnvironment env)
    {
        this.db = db;
        this.notify = notify;
        this.env = env;
    }


    private int Uid =>
        int.Parse(
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value
        );


    private bool EsTecnico =>
        User.IsInRole("tecnico");


    // =========================================================
    // LISTADO
    // =========================================================

    public async Task<IActionResult> Index()
    {
        // La bandeja muestra únicamente solicitudes del mes actual.
        // Las solicitudes anteriores se conservan en la base de datos y
        // siguen disponibles para reportes/historial, pero no saturan la bandeja.
        var ahoraLocal = DateTime.Now;
        var inicioMesUtc = new DateTime(ahoraLocal.Year, ahoraLocal.Month, 1, 0, 0, 0, DateTimeKind.Local).ToUniversalTime();
        var siguienteMesUtc = new DateTime(ahoraLocal.Year, ahoraLocal.Month, 1, 0, 0, 0, DateTimeKind.Local).AddMonths(1).ToUniversalTime();

        var q = db.Solicitudes
            .Include(x => x.Categoria)
            .Include(x => x.Equipo)
            .Include(x => x.Tecnico)
            .Where(x => x.CreadoEn >= inicioMesUtc && x.CreadoEn < siguienteMesUtc)
            .AsQueryable();


        if (User.IsInRole("usuario"))
        {
            // Usuario solamente ve sus propias solicitudes.
            q = q.Where(x =>
                x.UsuarioId == Uid
            );
        }
        else if (EsTecnico)
        {
            // Técnico:
            //
            // 1. Puede ver solicitudes todavía sin asignar,
            //    para poder decidir si las acepta.
            //
            // 2. Puede ver las que él mismo aceptó.
            //
            // 3. NO puede ver las aceptadas por otro técnico.

            q = q.Where(x =>
                x.TecnicoId == Uid ||
                x.TecnicoId == null
            );
        }
        else if (User.IsInRole("admin"))
        {
            // Administración puede ver todas las solicitudes.
        }
        else
        {
            return Forbid();
        }

        var solicitudes = await q
            .OrderByDescending(x => x.CreadoEn)
            .ToListAsync();


        return View(solicitudes);
    }


    // =========================================================
    // DETALLES
    // =========================================================

    public async Task<IActionResult> Details(int id)
    {
        var solicitud = await db.Solicitudes
            .Include(x => x.Usuario)
            .Include(x => x.Categoria)
            .Include(x => x.Equipo)
            .Include(x => x.Tecnico)
            .Include(x => x.TecnicoSolicitado)
            .Include(x => x.Adjuntos)
            .SingleOrDefaultAsync(
                x => x.Id == id
            );


        if (solicitud == null)
            return NotFound();


        // =====================================================
        // PERMISOS
        // =====================================================

        // El usuario creador siempre puede ver su solicitud.
        if (solicitud.UsuarioId == Uid)
        {
            // permitido
        }

        // El técnico asignado puede verla.
        else if (
            EsTecnico &&
            solicitud.TecnicoId == Uid)
        {
            // permitido
        }

        // Un técnico puede revisar una solicitud SIN ASIGNAR
        // antes de decidir si la acepta.
        else if (
            EsTecnico &&
            !solicitud.TecnicoId.HasValue)
        {
            // Cualquier técnico BT puede revisar una solicitud pendiente
            // y decidir si la acepta. El usuario no elige técnico.
        }

        // Administración puede consultar cualquier solicitud.
        else if (User.IsInRole("admin"))
        {
            ViewBag.Techs = await db.Usuarios
                .Where(x => x.Activo && x.Rol == Rol.tecnico)
                .OrderBy(x => x.Nombre)
                .ThenBy(x => x.Apellido)
                .ToListAsync();
        }
        else
        {
            return NotFound();
        }

        if (User.IsInRole("admin") && ViewBag.Techs == null)
        {
            ViewBag.Techs = await db.Usuarios
                .Where(x => x.Activo && x.Rol == Rol.tecnico)
                .OrderBy(x => x.Nombre)
                .ThenBy(x => x.Apellido)
                .ToListAsync();
        }

        return View(solicitud);
    }


    // =========================================================
    // CREAR SOLICITUD - GET
    // =========================================================

    [Authorize(Roles = "usuario")]
    public async Task<IActionResult> Create()
    {
        return View(
            await PrepareCreateVm(
                new SolicitudFormVm()
            )
        );
    }


    // =========================================================
    // CREAR SOLICITUD - POST
    // =========================================================

    [HttpPost]
    [Authorize(Roles = "usuario")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SolicitudFormVm m,
        List<IFormFile>? files)
    {
        // =====================================================
        // EQUIPO
        // =====================================================

        if (m.EquipoId.HasValue)
        {
            var equipoValido =
                await db.Equipos.AnyAsync(x =>
                    x.Id == m.EquipoId.Value &&
                    x.UsuarioId == Uid &&
                    x.Estado != EstadoEquipo.baja
                );


            if (!equipoValido)
            {
                ModelState.AddModelError(
                    nameof(m.EquipoId),
                    "El equipo seleccionado no es válido."
                );
            }
        }


        // =====================================================
        // MODALIDAD
        // =====================================================

        if (m.Modalidad ==
            ModalidadAtencion.presencial)
        {
            if (
                m.FechaVisitaSolicitada == null ||
                m.HoraVisitaSolicitada == null)
            {
                ModelState.AddModelError(
                    "",
                    "Para una atención en oficina debes indicar fecha y hora aproximada."
                );
            }
        }
        else
        {
            m.FechaVisitaSolicitada = null;
            m.HoraVisitaSolicitada = null;
        }


        if (string.IsNullOrWhiteSpace(m.AnyDeskId))
        {
            ModelState.AddModelError(nameof(m.AnyDeskId), "El ID o alias de AnyDesk es obligatorio.");
        }

        if (!ModelState.IsValid)
        {
            return await CreateWith(m);
        }


        // =====================================================
        // CATEGORÍA
        // =====================================================

        var categoria =
            await db.Categorias
                .SingleOrDefaultAsync(x =>
                    x.Id == m.CategoriaId &&
                    x.Activo == true
                );


        if (categoria == null)
        {
            ModelState.AddModelError(
                nameof(m.CategoriaId),
                "La categoría seleccionada no es válida."
            );

            return await CreateWith(m);
        }


        // =====================================================
        // CREAR SOLICITUD
        // =====================================================

        var solicitud = new Solicitud
        {
            Codigo =
                $"BT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(10, 99)}",

            UsuarioId = Uid,

            CategoriaId =
                m.CategoriaId,

            EquipoId =
                m.EquipoId,

            TecnicoSolicitadoId = null,

            Titulo =
                m.Titulo.Trim(),

            Descripcion =
                m.Descripcion.Trim(),

            Prioridad =
                m.Prioridad,

            PuedeContinuarTrabajando =
                m.PuedeContinuar,

            AnyDeskId =
                m.AnyDeskId.Trim(),

            ModalidadAtencion =
                m.Modalidad,

            FechaVisitaSolicitada =
                m.FechaVisitaSolicitada,

            HoraVisitaSolicitada =
                m.HoraVisitaSolicitada,

            FechaSolicitada =
                m.FechaVisitaSolicitada,

            HoraInicioSolicitada =
                m.HoraVisitaSolicitada
        };


        // Remoto no utiliza horario.
        if (
            m.Modalidad ==
            ModalidadAtencion.remoto)
        {
            solicitud.FechaVisitaSolicitada = null;
            solicitud.HoraVisitaSolicitada = null;
            solicitud.FechaSolicitada = null;
            solicitud.HoraInicioSolicitada = null;
        }


        db.Solicitudes.Add(solicitud);

        await db.SaveChangesAsync();

        // El aviso de nuevas solicitudes para BT y administración
        // se muestra directamente en el menú "Solicitudes".
        // No se crea una notificación aparte para estos roles.

        // =====================================================
        // ARCHIVOS
        // =====================================================

        await SaveFiles(
            solicitud,
            files
        );


        // =====================================================
        // HISTORIAL
        // =====================================================

        db.HistorialSolicitudes.Add(
            new HistorialSolicitud
            {
                SolicitudId =
                    solicitud.Id,

                UsuarioId =
                    Uid,

                EstadoNuevo =
                    solicitud.Estado.ToString(),

                Descripcion =
                    "Solicitud creada."
            }
        );


        await db.SaveChangesAsync();




        return RedirectToAction(
            nameof(Details),
            new { id = solicitud.Id }
        );
    }


    // =========================================================
    // PREPARAR FORMULARIO
    // =========================================================

    private async Task<SolicitudFormVm> PrepareCreateVm(
        SolicitudFormVm model)
    {
        model.Categorias =
            await db.Categorias
                .Where(x => x.Activo == true)
                .OrderBy(x => x.Nombre)
                .ToListAsync();


        model.Equipos =
            await db.Equipos
                .Where(x =>
                    x.UsuarioId == Uid &&
                    x.Estado != EstadoEquipo.baja
                )
                .OrderBy(x => x.Codigo)
                .ToListAsync();




        return model;
    }


    private async Task<IActionResult> CreateWith(
        SolicitudFormVm model)
    {
        model =
            await PrepareCreateVm(model);

        return View(
            "Create",
            model
        );
    }


    // =========================================================
    // GUARDAR ARCHIVOS
    // =========================================================

    private async Task SaveFiles(
        Solicitud solicitud,
        List<IFormFile>? files)
    {
        if (files == null)
            return;


        var root =
            Path.Combine(
                env.ContentRootPath,
                "Uploads"
            );


        Directory.CreateDirectory(root);


        foreach (
            var file in files
                .Where(x => x.Length > 0)
                .Take(6))
        {
            if (
                file.Length >
                5 * 1024 * 1024)
            {
                continue;
            }


            var extension =
                Path.GetExtension(
                    file.FileName
                ).ToLowerInvariant();


            var allowed = new[]
            {
                ".png",
                ".jpg",
                ".jpeg",
                ".gif",
                ".webp",
                ".pdf",
                ".txt",
                ".doc",
                ".docx",
                ".xls",
                ".xlsx",
                ".ppt",
                ".pptx",
                ".zip"
            };


            if (!allowed.Contains(extension))
                continue;


            var safeName =
                Guid.NewGuid() +
                extension;


            var path =
                Path.Combine(
                    root,
                    safeName
                );


            await using var stream =
                System.IO.File.Create(path);


            await file.CopyToAsync(stream);


            db.Adjuntos.Add(
                new Adjunto
                {
                    SolicitudId =
                        solicitud.Id,

                    UsuarioId =
                        Uid,

                    NombreOriginal =
                        Path.GetFileName(
                            file.FileName
                        ),

                    NombreArchivo =
                        safeName,

                    Ruta =
                        Path.Combine(
                            "Uploads",
                            safeName
                        ),

                    TipoMime =
                        file.ContentType,

                    Tamano =
                        file.Length
                }
            );
        }


        await db.SaveChangesAsync();
    }


    // =========================================================
    // ACTUALIZAR SOLICITUD
    // =========================================================

    [HttpPost]
    [Authorize(Roles = "tecnico,admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        EstadoSolicitud estado,
        DateOnly? fecha,
        TimeSpan? inicio,
        TimeSpan? fin,
        string? solucion,
        int? tecnicoId)
    {
        var solicitud =
            await db.Solicitudes
                .Include(x => x.Usuario)
                .SingleOrDefaultAsync(
                    x => x.Id == id
                );


        if (solicitud == null)
            return NotFound();


        // =====================================================
        // PERMISOS DE ACTUALIZACIÓN
        // =====================================================

        var esAdmin = User.IsInRole("admin");

        if (!esAdmin && solicitud.TecnicoId != Uid)
        {
            return NotFound();
        }

        var estadoAnterior = solicitud.Estado.ToString();
        var fueAsignadaPorAdmin = false;

        if (esAdmin && tecnicoId.HasValue)
        {
            if (solicitud.TecnicoId.HasValue)
            {
                TempData["Error"] = "La solicitud ya tiene un técnico asignado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var tecnicoValido = await db.Usuarios.AnyAsync(x =>
                x.Id == tecnicoId.Value &&
                x.Activo &&
                x.Rol == Rol.tecnico);

            if (!tecnicoValido)
            {
                TempData["Error"] = "El técnico seleccionado no es válido.";
                return RedirectToAction(nameof(Details), new { id });
            }

            solicitud.TecnicoId = tecnicoId.Value;
            fueAsignadaPorAdmin = true;
        }

        solicitud.Estado = fueAsignadaPorAdmin
            ? EstadoSolicitud.asignada
            : estado;

        solicitud.FechaProgramada = fecha;
        solicitud.HoraInicioProgramada = inicio;
        solicitud.HoraFinProgramada = fin;

        var puedeGuardarReporte = solicitud.Estado is EstadoSolicitud.en_proceso or EstadoSolicitud.resuelta or EstadoSolicitud.cerrada;
        if (puedeGuardarReporte && !string.IsNullOrWhiteSpace(solucion))
            solicitud.Solucion = solucion.Trim();
        else if (!puedeGuardarReporte)
            solicitud.Solucion = null;

        solicitud.ActualizadoEn = DateTime.UtcNow;

        if (solicitud.Estado == EstadoSolicitud.resuelta)
            solicitud.ResueltoEn = DateTime.UtcNow;

        if (solicitud.Estado == EstadoSolicitud.cerrada)
            solicitud.CerradoEn = DateTime.UtcNow;

        // =====================================================
        // HISTORIAL
        // =====================================================

        db.HistorialSolicitudes.Add(
            new HistorialSolicitud
            {
                SolicitudId =
                    id,

                UsuarioId =
                    Uid,

                EstadoAnterior =
                    estadoAnterior,

                EstadoNuevo =
                    solicitud.Estado.ToString(),

                Descripcion =
                    fueAsignadaPorAdmin
                        ? "Solicitud asignada por administración a personal de BT."
                        : $"Solicitud actualizada a {solicitud.Estado}."
            }
        );


        await db.SaveChangesAsync();

        if (esAdmin && tecnicoId.HasValue)
        {
            var tecnicoAsignado = await db.Usuarios
                .Where(x => x.Id == tecnicoId.Value)
                .Select(x => new { x.Nombre, x.Apellido })
                .SingleAsync();

            await notify.AddAsync(
                tecnicoId.Value,
                id,
                "asignacion",
                "Solicitud asignada",
                $"Administración te asignó la solicitud {solicitud.Codigo}.",
                $"/Solicitudes/Details/{id}"
            );

            await notify.AddAsync(
                solicitud.UsuarioId,
                id,
                "asignacion",
                "Solicitud asignada",
                $"Tu solicitud {solicitud.Codigo} fue asignada a {tecnicoAsignado.Nombre} {tecnicoAsignado.Apellido}.",
                $"/Solicitudes/Details/{id}"
            );
        }

        // =====================================================
        // NOTIFICAR AL SOLICITANTE
        // =====================================================
        //
        // El técnico NO recibe una notificación sobre su
        // propio cambio.
        //

        if (solicitud.UsuarioId != Uid && !fueAsignadaPorAdmin)
        {
            await notify.AddAsync(
                solicitud.UsuarioId,
                id,
                "cambio",
                "Actualización del técnico",
                $"El técnico actualizó {solicitud.Codigo}. Estado: {solicitud.Estado}.",
                $"/Solicitudes/Details/{id}"
            );
        }


        return RedirectToAction(
            nameof(Details),
            new { id }
        );
    }


    // =========================================================
    // CANCELAR SOLICITUD (USUARIO)
    // =========================================================

    [HttpPost]
    [Authorize(Roles = "usuario")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var solicitud = await db.Solicitudes.SingleOrDefaultAsync(x => x.Id == id && x.UsuarioId == Uid);
        if (solicitud == null) return NotFound();

        if (solicitud.Estado is EstadoSolicitud.resuelta or EstadoSolicitud.cerrada or EstadoSolicitud.cancelada)
        {
            TempData["Info"] = "Esta solicitud ya no puede cancelarse.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var estadoAnterior = solicitud.Estado.ToString();
        solicitud.Estado = EstadoSolicitud.cancelada;
        solicitud.ActualizadoEn = DateTime.UtcNow;
        solicitud.CerradoEn = DateTime.UtcNow;

        db.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = id,
            UsuarioId = Uid,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = EstadoSolicitud.cancelada.ToString(),
            Descripcion = "Solicitud cancelada por el usuario solicitante."
        });

        await db.SaveChangesAsync();

        if (solicitud.TecnicoId.HasValue)
        {
            await notify.AddAsync(
                solicitud.TecnicoId.Value, id, "cancelacion", "Solicitud cancelada",
                $"La solicitud {solicitud.Codigo} fue cancelada por el usuario.",
                $"/Solicitudes/Details/{id}");
        }

        TempData["Success"] = "La solicitud fue cancelada.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // =========================================================
    // LIBERAR SOLICITUD (TÉCNICO)
    // =========================================================

    [HttpPost]
    [Authorize(Roles = "tecnico")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(int id)
    {
        var solicitud = await db.Solicitudes.SingleOrDefaultAsync(x => x.Id == id && x.TecnicoId == Uid);
        if (solicitud == null) return NotFound();

        if (solicitud.Estado is EstadoSolicitud.resuelta or EstadoSolicitud.cerrada or EstadoSolicitud.cancelada)
        {
            TempData["Info"] = "Una solicitud finalizada no puede devolverse a la bandeja.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var estadoAnterior = solicitud.Estado.ToString();
        solicitud.TecnicoId = null;
        solicitud.Estado = EstadoSolicitud.pendiente;
        solicitud.FechaProgramada = null;
        solicitud.HoraInicioProgramada = null;
        solicitud.HoraFinProgramada = null;
        solicitud.Solucion = null;
        solicitud.ActualizadoEn = DateTime.UtcNow;

        db.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = id,
            UsuarioId = Uid,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = EstadoSolicitud.pendiente.ToString(),
            Descripcion = "El técnico liberó la solicitud para que pueda ser reasignada."
        });

        await db.SaveChangesAsync();

        await notify.AddAsync(
            solicitud.UsuarioId, id, "reasignacion", "Solicitud devuelta a la bandeja",
            $"La solicitud {solicitud.Codigo} fue liberada por el técnico y volverá a estar disponible para asignación.",
            $"/Solicitudes/Details/{id}");

        TempData["Success"] = "La solicitud fue liberada y puede ser reasignada a otro técnico.";
        return RedirectToAction(nameof(Index));
    }


    // =========================================================
    // TOMAR SOLICITUD
    // =========================================================

    [HttpPost]
    [Authorize(Roles = "tecnico")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(int id)
    {
        // =====================================================
        // ASIGNACIÓN ATÓMICA
        // =====================================================
        //
        // Si dos técnicos intentan aceptar al mismo tiempo,
        // solamente uno podrá actualizar la fila.
        //

        var filasAfectadas =
            await db.Solicitudes
                .Where(x =>
                    x.Id == id &&
                    x.TecnicoId == null &&
                    !x.TecnicoSolicitadoId.HasValue
                )
                .ExecuteUpdateAsync(setters =>
                    setters
                        .SetProperty(
                            x => x.TecnicoId,
                            Uid
                        )
                        .SetProperty(
                            x => x.Estado,
                            EstadoSolicitud.asignada
                        )
                        .SetProperty(
                            x => x.ActualizadoEn,
                            DateTime.UtcNow
                        )
                );


        // =====================================================
        // YA FUE TOMADA
        // =====================================================

        if (filasAfectadas == 0)
        {
            TempData["Info"] =
                "Esta solicitud ya fue aceptada por otro técnico.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =====================================================
        // OBTENER SOLICITUD
        // =====================================================

        var solicitud =
            await db.Solicitudes
                .Include(x => x.Usuario)
                .SingleOrDefaultAsync(
                    x => x.Id == id
                );


        if (solicitud == null)
            return NotFound();




        // =====================================================
        // HISTORIAL
        // =====================================================

        db.HistorialSolicitudes.Add(
            new HistorialSolicitud
            {
                SolicitudId =
                    id,

                UsuarioId =
                    Uid,

                EstadoAnterior =
                    EstadoSolicitud.pendiente.ToString(),

                EstadoNuevo =
                    EstadoSolicitud.asignada.ToString(),

                Descripcion =
                    User.IsInRole("admin")
                        ? "Solicitud aceptada por administración."
                        : "Solicitud aceptada por técnico de BT."
            }
        );


        await db.SaveChangesAsync();


        // =====================================================
        // NOTIFICAR AL SOLICITANTE
        // =====================================================
        //
        // El técnico que aceptó NO recibe esta notificación.
        //

        if (solicitud.UsuarioId != Uid)
        {
            await notify.AddAsync(
                solicitud.UsuarioId,
                id,
                "asignacion",
                "Solicitud aceptada",
                $"{solicitud.Codigo} fue aceptada por {User.Identity?.Name ?? "personal de BT"}.",
                $"/Solicitudes/Details/{id}"
            );
        }


        TempData["Success"] =
            $"{solicitud.Codigo} fue aceptada correctamente.";


        return RedirectToAction(
            nameof(Details),
            new { id }
        );
    }
}


// =============================================================
// VIEW MODEL
// =============================================================

public class SolicitudFormVm
{
    [Required]
    public int CategoriaId { get; set; }


    public int? EquipoId { get; set; }



    [Required]
    [StringLength(180)]
    public string Titulo { get; set; } = "";


    [Required]
    public string Descripcion { get; set; } = "";


    public Prioridad Prioridad { get; set; }
        = Prioridad.media;


    public bool PuedeContinuar { get; set; }
        = true;

    [Required]
    [StringLength(100)]
    public string AnyDeskId { get; set; } = "";


    public ModalidadAtencion Modalidad { get; set; }
        = ModalidadAtencion.remoto;


    public DateOnly? FechaVisitaSolicitada { get; set; }


    public TimeSpan? HoraVisitaSolicitada { get; set; }


    public List<Categoria> Categorias { get; set; }
        = new();


    public List<Equipo> Equipos { get; set; }
        = new();


}