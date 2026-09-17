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
        var q = db.Solicitudes
            .Include(x => x.Categoria)
            .Include(x => x.Equipo)
            .Include(x => x.Tecnico)
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
                x.TecnicoId == null ||
                x.TecnicoId == Uid
            );
        }
        else
        {
            // Admin no participa en las solicitudes.
            //
            // No puede ver solicitudes desde este módulo.
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
            !solicitud.TecnicoId.HasValue &&
            (!solicitud.TecnicoSolicitadoId.HasValue ||
             solicitud.TecnicoSolicitadoId.Value == Uid))
        {
            // permitido
        }

        // Cualquier otra persona no puede verla.
        else
        {
            return NotFound();
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
        // TÉCNICO SOLICITADO
        // =====================================================

        if (m.TecnicoSolicitadoId.HasValue)
        {
            var tecnicoValido =
                await db.Usuarios.AnyAsync(x =>
                    x.Id == m.TecnicoSolicitadoId.Value &&
                    x.Activo == true &&
                    (
                        x.Rol == Rol.tecnico ||
                        x.Rol == Rol.admin
                    )
                );


            if (!tecnicoValido)
            {
                ModelState.AddModelError(
                    nameof(m.TecnicoSolicitadoId),
                    "Técnico no válido."
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

            TecnicoSolicitadoId =
                m.TecnicoSolicitadoId,

            Titulo =
                m.Titulo.Trim(),

            Descripcion =
                m.Descripcion.Trim(),

            Prioridad =
                m.Prioridad,

            PuedeContinuarTrabajando =
                m.PuedeContinuar,

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


        model.Tecnicos =
            await db.Usuarios
                .Where(x =>
                    x.Activo == true &&
                    (
                        x.Rol == Rol.tecnico ||
                        x.Rol == Rol.admin
                    )
                )
                .OrderBy(x => x.Nombre)
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
    [Authorize(Roles = "tecnico")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        EstadoSolicitud estado,
        DateOnly? fecha,
        TimeSpan? inicio,
        TimeSpan? fin,
        string? solucion,
        string? comentario)
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
        // SOLO EL TÉCNICO ASIGNADO
        // =====================================================

        if (solicitud.TecnicoId != Uid)
        {
            return NotFound();
        }


        var estadoAnterior =
            solicitud.Estado.ToString();


        solicitud.Estado =
            estado;


        solicitud.FechaProgramada =
            fecha;


        solicitud.HoraInicioProgramada =
            inicio;


        solicitud.HoraFinProgramada =
            fin;


        solicitud.Solucion =
            solucion;


        solicitud.ActualizadoEn =
            DateTime.UtcNow;


        if (
            estado ==
            EstadoSolicitud.resuelta)
        {
            solicitud.ResueltoEn =
                DateTime.UtcNow;
        }


        if (
            estado ==
            EstadoSolicitud.cerrada)
        {
            solicitud.CerradoEn =
                DateTime.UtcNow;
        }


        // =====================================================
        // COMENTARIO
        // =====================================================

        if (!string.IsNullOrWhiteSpace(comentario))
        {
            db.Comentarios.Add(
                new Comentario
                {
                    SolicitudId =
                        id,

                    UsuarioId =
                        Uid,

                    ComentarioTexto =
                        comentario.Trim()
                }
            );
        }


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
                    estado.ToString(),

                Descripcion =
                    $"Solicitud actualizada a {estado}."
            }
        );


        await db.SaveChangesAsync();


        // =====================================================
        // NOTIFICAR AL SOLICITANTE
        // =====================================================
        //
        // El técnico NO recibe una notificación sobre su
        // propio cambio.
        //

        if (solicitud.UsuarioId != Uid)
        {
            await notify.AddAsync(
                solicitud.UsuarioId,
                id,
                "cambio",
                "Actualización del técnico",
                $"El técnico actualizó {solicitud.Codigo}. Estado: {estado}.",
                $"/Solicitudes/Details/{id}"
            );
        }


        return RedirectToAction(
            nameof(Details),
            new { id }
        );
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
                    (!x.TecnicoSolicitadoId.HasValue ||
                     x.TecnicoSolicitadoId.Value == Uid)
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
                    "Solicitud aceptada por técnico de BT."
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
                $"{solicitud.Codigo} fue tomada por un técnico de BT.",
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


    public int? TecnicoSolicitadoId { get; set; }


    [Required]
    [StringLength(180)]
    public string Titulo { get; set; } = "";


    [Required]
    public string Descripcion { get; set; } = "";


    public Prioridad Prioridad { get; set; }
        = Prioridad.media;


    public bool PuedeContinuar { get; set; }
        = true;


    public ModalidadAtencion Modalidad { get; set; }
        = ModalidadAtencion.remoto;


    public DateOnly? FechaVisitaSolicitada { get; set; }


    public TimeSpan? HoraVisitaSolicitada { get; set; }


    public List<Categoria> Categorias { get; set; }
        = new();


    public List<Equipo> Equipos { get; set; }
        = new();


    public List<Usuario> Tecnicos { get; set; }
        = new();
}