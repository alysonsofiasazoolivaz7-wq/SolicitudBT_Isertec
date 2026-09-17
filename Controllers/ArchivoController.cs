using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using SolicitudIT_Isertec.Data;
namespace SolicitudIT_Isertec.Controllers;[Authorize]public class ArchivoController:Controller{readonly AppDbContext db;readonly IWebHostEnvironment env;public ArchivoController(AppDbContext db,IWebHostEnvironment env){this.db=db;this.env=env;}int U=>int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);public async Task<IActionResult> Download(int id){var a=await db.Adjuntos.Include(x=>x.Solicitud).SingleOrDefaultAsync(x=>x.Id==id);if(a==null)return NotFound();var esSolicitante = a.Solicitud.UsuarioId == U;
var esTecnicoAsignado = User.IsInRole("tecnico") && a.Solicitud.TecnicoId == U;
var esTecnicoRevisando = User.IsInRole("tecnico") && !a.Solicitud.TecnicoId.HasValue;

if (!esSolicitante && !esTecnicoAsignado && !esTecnicoRevisando)
    return Forbid();var path=Path.Combine(env.ContentRootPath,a.Ruta);if(!System.IO.File.Exists(path))return NotFound();return PhysicalFile(path,a.TipoMime??"application/octet-stream",a.NombreOriginal);}}
