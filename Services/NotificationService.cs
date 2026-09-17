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

    /*
     * Notificaciones normales.
     *
     * Las notificaciones de chat NO cuentan aquí porque
     * se muestran mediante el indicador del menú Chat.
     */
    public Task<int> UnreadAsync(int userId)
    {
        return db.Notificaciones
            .CountAsync(x =>
                x.UsuarioId == userId &&
                !x.LeidaEn.HasValue &&
                x.Tipo != "chat"
            );
    }

    /*
     * Mensajes privados pendientes de leer.
     *
     * El indicador de Chat se basa directamente en MensajePrivado.
     * Esto evita depender de que exista una Notificacion de tipo chat.
     */
    public Task<int> UnreadChatAsync(int userId)
    {
        return db.MensajesPrivados
            .CountAsync(x =>
                x.DestinatarioId == userId &&
                !x.LeidoEn.HasValue
            );
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

    /*
     * Marcar como leídas solamente las notificaciones normales.
     *
     * Las notificaciones de chat quedan fuera porque el estado
     * real de lectura lo controla MensajePrivado.LeidoEn.
     */
    public async Task MarkAllAsReadAsync(int userId)
    {
        await db.Notificaciones
            .Where(x =>
                x.UsuarioId == userId &&
                !x.LeidaEn.HasValue &&
                x.Tipo != "chat"
            )
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    x => x.LeidaEn,
                    DateTime.UtcNow
                )
            );
    }

}
