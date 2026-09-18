using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;
using SolicitudIT_Isertec.Models;

namespace SolicitudIT_Isertec.Services;

public class NotificationService
{
    private readonly AppDbContext db;

public NotificationService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task AddAsync(
        int userId,
        int? requestId,
        string type,
        string title,
        string message,
        string? link = null)
    {
        var notification = new Notificacion
        {
            UsuarioId = userId,
            SolicitudId = requestId,
            Tipo = type,
            Titulo = title,
            Mensaje = message,
            Enlace = link,
            CreadoEn = DateTime.UtcNow
        };

        db.Notificaciones.Add(notification);

        await db.SaveChangesAsync();
    }
    public Task<int> UnreadAsync(int userId)
    {
        return db.Notificaciones
            .CountAsync(x =>
                x.UsuarioId == userId &&
                !x.LeidaEn.HasValue
            );
    }

    public async Task<int> UnreadSolicitudesAsync(int userId)
    {
        var usuario = await db.Usuarios
            .Where(x => x.Id == userId)
            .Select(x => new { x.Rol })
            .SingleOrDefaultAsync();

        if (usuario == null)
            return 0;

        var query = db.Solicitudes
            .Where(x => x.TecnicoId == null);

        if (usuario.Rol == Rol.tecnico)
        {
            query = query.Where(x =>
                !x.TecnicoSolicitadoId.HasValue ||
                x.TecnicoSolicitadoId == userId);
        }
        else if (usuario.Rol != Rol.admin)
        {
            return 0;
        }

        return await query.CountAsync();
    }

    public async Task MarkAsReadAsync(
        int notificationId,
        int userId)
    {
        var notification =
            await db.Notificaciones
                .SingleOrDefaultAsync(x =>
                    x.Id == notificationId &&
                    x.UsuarioId == userId
                );

        if (notification == null)
            return;

        notification.LeidaEn = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
    public async Task MarkAllAsReadAsync(int userId)
    {
        await db.Notificaciones
            .Where(x =>
                x.UsuarioId == userId &&
                !x.LeidaEn.HasValue
            )
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    x => x.LeidaEn,
                    DateTime.UtcNow
                )
            );
    }

}
