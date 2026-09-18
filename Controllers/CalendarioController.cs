using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;

namespace SolicitudIT_Isertec.Controllers;

[Authorize(Roles = "tecnico,admin")]
public class CalendarioController : Controller
{
    private readonly AppDbContext db;

    public CalendarioController(AppDbContext db) => this.db = db;

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var query = db.Solicitudes
            .Include(x => x.Usuario)
            .Include(x => x.Tecnico)
            .Where(x => x.FechaProgramada.HasValue);

        // El administrador ve todas las solicitudes programadas.
        // Cada técnico solamente ve las solicitudes que él aceptó.
        if (User.IsInRole("tecnico"))
        {
            query = query.Where(x => x.TecnicoId == userId);
        }

        var solicitudes = await query
            .OrderBy(x => x.FechaProgramada)
            .ThenBy(x => x.HoraInicioProgramada)
            .ToListAsync();

        return View(solicitudes);
    }
}
