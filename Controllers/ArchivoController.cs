using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;

namespace SolicitudIT_Isertec.Controllers;

[Authorize]
public class ArchivoController : Controller
{
    private readonly AppDbContext db;
    private readonly IWebHostEnvironment env;

    public ArchivoController(AppDbContext db, IWebHostEnvironment env)
    {
        this.db = db;
        this.env = env;
    }

    private int UserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }
    }

    public async Task<IActionResult> Download(int id)
    {
        var attachment = await db.Adjuntos
            .Include(x => x.Solicitud)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (attachment == null)
            return NotFound();

        var userId = UserId;

        // Los adjuntos de una solicitud solo son accesibles por sus
        // dos participantes: creador y técnico asignado.
        var allowed =
            attachment.Solicitud.UsuarioId == userId ||
            attachment.Solicitud.TecnicoId == userId;

        if (!allowed)
            return Forbid();

        if (string.IsNullOrWhiteSpace(env.WebRootPath))
            return NotFound();

        var fileName = Path.GetFileName(attachment.NombreArchivo);
        var path = Path.Combine(env.WebRootPath, "Uploads", fileName);

        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(
            path,
            attachment.TipoMime ?? "application/octet-stream",
            attachment.NombreOriginal);
    }
}
