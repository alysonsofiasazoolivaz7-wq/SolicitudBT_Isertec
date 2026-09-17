using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;
using SolicitudIT_Isertec.Models;

namespace SolicitudIT_Isertec.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly AppDbContext db;
    private readonly IWebHostEnvironment env;

    public ChatController(
        AppDbContext db,
        IWebHostEnvironment env)
    {
        this.db = db;
        this.env = env;
    }

    private int U =>
        int.Parse(
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )!.Value
        );

    private bool EsTecnico =>
        User.IsInRole("tecnico");

    private async Task<Solicitud?> ObtenerSolicitudParticipante(int id)
    {
        var solicitud = await db.Solicitudes
            .Include(x => x.Usuario)
            .Include(x => x.Tecnico)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (solicitud == null)
            return null;

        // El administrador no participa en chats privados.
        if (User.IsInRole("admin"))
            return null;

        // El usuario solo puede entrar a sus propias solicitudes
        // cuando ya existe un técnico asignado.
        if (User.IsInRole("usuario"))
        {
            if (solicitud.UsuarioId != U ||
                !solicitud.TecnicoId.HasValue)
                return null;

            return solicitud;
        }

        // Un técnico solamente puede entrar a las solicitudes
        // que él mismo aceptó/tiene asignadas.
        if (EsTecnico &&
            solicitud.TecnicoId == U)
        {
            return solicitud;
        }

        return null;
    }

    public async Task<IActionResult> Index(int? id)
    {
        if (User.IsInRole("admin"))
            return Forbid();

        IQueryable<Solicitud> query = db.Solicitudes
            .Include(x => x.Usuario)
            .Include(x => x.Tecnico);

        if (User.IsInRole("usuario"))
        {
            query = query.Where(x =>
                x.UsuarioId == U &&
                x.TecnicoId.HasValue);
        }
        else if (EsTecnico)
        {
            query = query.Where(x =>
                x.TecnicoId == U);
        }
        else
        {
            return Forbid();
        }

        var solicitudes = await query
            .OrderByDescending(x => x.ActualizadoEn)
            .ToListAsync();

        var selectedId =
            id.HasValue &&
            solicitudes.Any(x => x.Id == id.Value)
                ? id
                : solicitudes.FirstOrDefault()?.Id;

        return View(
            new ChatVm
            {
                Requests = solicitudes,
                SelectedId = selectedId,
                Messages = selectedId.HasValue
                    ? await ObtenerMensajesAsync(selectedId.Value, true)
                    : new List<MensajePrivado>()
            }
        );
    }

    private async Task<List<MensajePrivado>> ObtenerMensajesAsync(
        int solicitudId,
        bool marcarLeidos)
    {
        var solicitud =
            await ObtenerSolicitudParticipante(solicitudId);

        if (solicitud == null)
            return new List<MensajePrivado>();

        var usuarioId = solicitud.UsuarioId;
        var tecnicoId = solicitud.TecnicoId!.Value;

        var mensajes = await db.MensajesPrivados
            .Include(x => x.Remitente)
            .Include(x => x.Adjunto)
            .Where(x =>
                x.SolicitudId == solicitudId &&
                (
                    (x.RemitenteId == usuarioId &&
                     x.DestinatarioId == tecnicoId) ||
                    (x.RemitenteId == tecnicoId &&
                     x.DestinatarioId == usuarioId)
                )
            )
            .OrderBy(x => x.CreadoEn)
            .ToListAsync();

        if (marcarLeidos)
        {
            var pendientes = mensajes
                .Where(x =>
                    x.DestinatarioId == U &&
                    !x.LeidoEn.HasValue)
                .ToList();

            if (pendientes.Count > 0)
            {
                foreach (var mensaje in pendientes)
                    mensaje.LeidoEn = DateTime.UtcNow;

                await db.SaveChangesAsync();
            }
        }

        return mensajes;
    }

    [HttpGet]
    public async Task<IActionResult> MessagesJson(int id)
    {
        var solicitud =
            await ObtenerSolicitudParticipante(id);

        if (solicitud == null)
            return NotFound();

        var mensajes =
            await ObtenerMensajesAsync(id, true);

        return Json(
            mensajes.Select(x => new
            {
                x.Id,
                x.Mensaje,
                x.CreadoEn,
                Remitente =
                    $"{x.Remitente.Nombre} {x.Remitente.Apellido}",
                x.RemitenteId,
                EsMio = x.RemitenteId == U,
                Adjunto = x.Adjunto == null
                    ? null
                    : new
                    {
                        x.Adjunto.Id,
                        x.Adjunto.NombreOriginal
                    }
            })
        );
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(
        int solicitudId,
        string? mensaje,
        IFormFile? file)
    {
        var solicitud =
            await ObtenerSolicitudParticipante(solicitudId);

        if (solicitud == null)
            return NotFound();

        var texto =
            mensaje?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(texto) &&
            (file == null || file.Length == 0))
        {
            return RedirectToAction(
                nameof(Index),
                new { id = solicitudId }
            );
        }

        var destinatarioId =
            solicitud.UsuarioId == U
                ? solicitud.TecnicoId!.Value
                : solicitud.UsuarioId;

        int? adjuntoId = null;

        if (file != null && file.Length > 0)
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                TempData["Info"] =
                    "El archivo no puede superar los 10 MB.";

                return RedirectToAction(
                    nameof(Index),
                    new { id = solicitudId }
                );
            }

            var extension =
                Path.GetExtension(
                    file.FileName
                ).ToLowerInvariant();

            var extensionesPermitidas = new[]
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

            if (!extensionesPermitidas.Contains(extension))
            {
                TempData["Info"] =
                    "El tipo de archivo no está permitido.";

                return RedirectToAction(
                    nameof(Index),
                    new { id = solicitudId }
                );
            }

            var carpeta =
                Path.Combine(
                    env.ContentRootPath,
                    "Uploads"
                );

            Directory.CreateDirectory(carpeta);

            var nombreArchivo =
                $"{Guid.NewGuid():N}{extension}";

            var rutaFisica =
                Path.Combine(
                    carpeta,
                    nombreArchivo
                );

            await using (
                var stream =
                    System.IO.File.Create(rutaFisica))
            {
                await file.CopyToAsync(stream);
            }

            var adjunto = new Adjunto
            {
                SolicitudId = solicitud.Id,
                UsuarioId = U,
                NombreOriginal =
                    Path.GetFileName(file.FileName),
                NombreArchivo = nombreArchivo,
                Ruta =
                    Path.Combine(
                        "Uploads",
                        nombreArchivo
                    ),
                TipoMime = file.ContentType,
                Tamano = file.Length
            };

            db.Adjuntos.Add(adjunto);
            await db.SaveChangesAsync();

            adjuntoId = adjunto.Id;
        }

        db.MensajesPrivados.Add(
            new MensajePrivado
            {
                SolicitudId = solicitud.Id,
                RemitenteId = U,
                DestinatarioId = destinatarioId,
                AdjuntoId = adjuntoId,
                Mensaje = texto
            }
        );

        solicitud.ActualizadoEn =
            DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Los mensajes de chat NO generan una tarjeta de
        // notificación. El indicador de Chat se basa en
        // MensajePrivado.LeidoEn.
        return RedirectToAction(
            nameof(Index),
            new { id = solicitudId }
        );
    }
}

public class ChatVm
{
    public List<Solicitud> Requests { get; set; } = new();

    public int? SelectedId { get; set; }

    public List<MensajePrivado> Messages { get; set; } = new();
}
