using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;

namespace SolicitudIT_Isertec.Controllers;

[Authorize(Roles = "usuario")]
public class NotificacionesController : Controller
{
    private readonly AppDbContext db;

    public NotificacionesController(AppDbContext db)
    {
        this.db = db;
    }

    private int UserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }
    }

    public async Task<IActionResult> Index()
    {
        var notifications = await db.Notificaciones
            .Where(x => x.UsuarioId == UserId)
            .OrderByDescending(x => x.CreadoEn)
            .Take(100)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(int id)
    {
        var notification = await db.Notificaciones
            .SingleOrDefaultAsync(x =>
                x.Id == id &&
                x.UsuarioId == UserId);

        if (notification != null)
        {
            notification.LeidaEn = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll()
    {
        await db.Notificaciones
            .Where(x =>
                x.UsuarioId == UserId &&
                !x.LeidaEn.HasValue)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    x => x.LeidaEn,
                    DateTime.UtcNow));

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ViewRequest(int id)
    {
        var notification = await db.Notificaciones
            .SingleOrDefaultAsync(x =>
                x.Id == id &&
                x.UsuarioId == UserId);

        if (notification == null || !notification.SolicitudId.HasValue)
            return NotFound();

        var solicitud = await db.Solicitudes
            .SingleOrDefaultAsync(x =>
                x.Id == notification.SolicitudId.Value &&
                x.UsuarioId == UserId);

        if (solicitud == null)
            return NotFound();

        notification.LeidaEn ??= DateTime.UtcNow;
        await db.SaveChangesAsync();

        return RedirectToAction(
            "Details",
            "Solicitudes",
            new { id = solicitud.Id });
    }
}
