using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;
using SolicitudIT_Isertec.Models;
using SolicitudIT_Isertec.Services;

namespace SolicitudIT_Isertec.Controllers;

[Authorize]
public class NotificacionesController : Controller
{
    private readonly AppDbContext db;
    private readonly NotificationService notify;

    public NotificacionesController(
        AppDbContext db,
        NotificationService notify)
    {
        this.db = db;
        this.notify = notify;
    }


    private int U =>
        int.Parse(
            User.FindFirst(
                ClaimTypes.NameIdentifier
            )!.Value
        );


    private bool EsTecnico =>
        User.IsInRole("tecnico");


    // =========================================================
    // LISTADO
    // =========================================================

    public async Task<IActionResult> Index()
    {
        var notificaciones =
            await db.Notificaciones
                .Where(x =>
                    x.UsuarioId == U
                )
                .OrderByDescending(
                    x => x.CreadoEn
                )
                .Take(100)
                .ToListAsync();


        return View(notificaciones);
    }


    // =========================================================
    // MARCAR UNA COMO LEÍDA
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(int id)
    {
        var notificacion =
            await db.Notificaciones
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == id &&
                        x.UsuarioId == U
                );


        if (notificacion != null)
        {
            notificacion.LeidaEn =
                DateTime.UtcNow;

            await db.SaveChangesAsync();
        }


        return RedirectToAction(
            nameof(Index)
        );
    }


    // =========================================================
    // MARCAR TODAS COMO LEÍDAS
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll()
    {
        await db.Notificaciones
            .Where(x =>
                x.UsuarioId == U &&
                !x.LeidaEn.HasValue
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters.SetProperty(
                        x => x.LeidaEn,
                        DateTime.UtcNow
                    )
            );


        return RedirectToAction(
            nameof(Index)
        );
    }


    // =========================================================
    // VISUALIZAR SOLICITUD
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> ViewRequest(
        int id)
    {
        var notificacion =
            await db.Notificaciones
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == id &&
                        x.UsuarioId == U
                );


        if (notificacion == null)
            return NotFound();

        // Los mensajes de chat no se abren desde el centro
        // de notificaciones. El indicador de Chat los gestiona
        // directamente mediante MensajePrivado.LeidoEn.
        if (notificacion.Tipo == "chat")
            return NotFound();


        if (!notificacion.SolicitudId.HasValue)
        {
            return RedirectToAction(
                nameof(Index)
            );
        }


        var solicitud =
            await db.Solicitudes
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                        notificacion.SolicitudId.Value
                );


        if (solicitud == null)
            return NotFound();


        // =====================================================
        // COMPROBAR PERMISOS
        // =====================================================

        var esSolicitante =
            solicitud.UsuarioId == U;


        var esTecnicoAsignado =
            EsTecnico &&
            solicitud.TecnicoId == U;


        // Técnico puede revisar una solicitud sin asignar
        // antes de aceptarla.
        var esTecnicoRevisando =
            EsTecnico &&
            !solicitud.TecnicoId.HasValue;


        if (!esSolicitante &&
            !esTecnicoAsignado &&
            !esTecnicoRevisando)
        {
            return NotFound();
        }


        notificacion.LeidaEn ??=
            DateTime.UtcNow;


        await db.SaveChangesAsync();


        return RedirectToAction(
            "Details",
            "Solicitudes",
            new
            {
                id =
                    solicitud.Id
            }
        );
    }


    // =========================================================
    // ACEPTAR SOLICITUD
    // =========================================================

    [HttpPost]
    [Authorize(Roles = "tecnico")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(
        int id)
    {
        // =====================================================
        // OBTENER NOTIFICACIÓN
        // =====================================================

        var notificacion =
            await db.Notificaciones
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == id &&
                        x.UsuarioId == U
                );


        if (notificacion == null)
            return NotFound();

        if (notificacion.Tipo != "nueva")
            return NotFound();


        if (!notificacion.SolicitudId.HasValue)
        {
            return RedirectToAction(
                nameof(Index)
            );
        }


        var solicitudId =
            notificacion.SolicitudId.Value;


        // =====================================================
        // ASIGNACIÓN ATÓMICA
        // =====================================================
        //
        // La condición TecnicoId == null garantiza que el
        // primer técnico que acepte sea el único que la toma.
        //

        var filasAfectadas =
            await db.Solicitudes
                .Where(x =>
                    x.Id == solicitudId &&
                    x.TecnicoId == null &&
                    (
                        !x.TecnicoSolicitadoId.HasValue ||
                        x.TecnicoSolicitadoId == U
                    )
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                x => x.TecnicoId,
                                U
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
        // OTRO TÉCNICO LA TOMÓ PRIMERO
        // =====================================================

        if (filasAfectadas == 0)
        {
            var solicitudActual =
                await db.Solicitudes
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == solicitudId);

            notificacion.LeidaEn ??=
                DateTime.UtcNow;

            await db.SaveChangesAsync();

            TempData["Info"] =
                solicitudActual?.TecnicoId != null
                    ? "Esta solicitud ya fue aceptada por otro técnico."
                    : solicitudActual?.TecnicoSolicitadoId.HasValue &&
                      solicitudActual.TecnicoSolicitadoId != U
                        ? "Esta solicitud fue dirigida a otro técnico de BT."
                        : "La solicitud ya no está disponible para ser aceptada.";

            return RedirectToAction(
                nameof(Index)
            );
        }


        // =====================================================
        // OBTENER SOLICITUD ACTUALIZADA
        // =====================================================

        var solicitud =
            await db.Solicitudes
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                        solicitudId
                );


        if (solicitud == null)
            return NotFound();


        // =====================================================
        // MARCAR NOTIFICACIÓN ACTUAL
        // =====================================================

        notificacion.LeidaEn =
            DateTime.UtcNow;


        // =====================================================
        // CERRAR NOTIFICACIONES DE LOS DEMÁS TÉCNICOS
        // =====================================================
        //
        // No eliminamos registros históricos.
        // Simplemente dejan de estar pendientes.
        //

        await db.Notificaciones
            .Where(x =>
                x.SolicitudId == solicitudId &&
                x.UsuarioId != U &&
                x.Tipo == "nueva" &&
                !x.LeidaEn.HasValue
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters.SetProperty(
                        x => x.LeidaEn,
                        DateTime.UtcNow
                    )
            );


        // =====================================================
        // HISTORIAL
        // =====================================================

        db.HistorialSolicitudes.Add(
            new HistorialSolicitud
            {
                SolicitudId =
                    solicitudId,

                UsuarioId =
                    U,

                EstadoAnterior =
                    EstadoSolicitud.pendiente.ToString(),

                EstadoNuevo =
                    EstadoSolicitud.asignada.ToString(),

                Descripcion =
                    "Solicitud aceptada por técnico de BT."
            }
        );


        await db.SaveChangesAsync();


        // =====================================================
        // NOTIFICAR AL SOLICITANTE
        // =====================================================
        //
        // IMPORTANTE:
        // el técnico que aceptó NO recibe esta notificación.
        //

        if (solicitud.UsuarioId != U)
        {
            await notify.AddAsync(
                solicitud.UsuarioId,
                solicitud.Id,
                "asignacion",
                "Solicitud aceptada",
                $"{solicitud.Codigo} fue tomada por un técnico de BT.",
                $"/Solicitudes/Details/{solicitud.Id}"
            );
        }


        TempData["Success"] =
            $"{solicitud.Codigo} fue aceptada correctamente.";


        return RedirectToAction(
            "Details",
            "Solicitudes",
            new
            {
                id =
                    solicitud.Id
            }
        );
    }
}