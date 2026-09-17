using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using SolicitudIT_Isertec.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SolicitudIT_Isertec.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<UsuarioPermiso> UsuarioPermisos => Set<UsuarioPermiso>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Comentario> Comentarios => Set<Comentario>();
    public DbSet<Adjunto> Adjuntos => Set<Adjunto>();
    public DbSet<MensajePrivado> MensajesPrivados => Set<MensajePrivado>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<HistorialSolicitud> HistorialSolicitudes => Set<HistorialSolicitud>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // =========================================================
        // USUARIOS
        // =========================================================

        b.Entity<Usuario>().ToTable("usuarios");

        Map(
            b.Entity<Usuario>(),
            new()
            {
                ["Id"] = "id",
                ["Nombre"] = "nombre",
                ["Apellido"] = "apellido",
                ["UsuarioLogin"] = "usuario",
                ["Email"] = "email",
                ["Password"] = "password",
                ["Rol"] = "rol",
                ["Activo"] = "activo",
                ["CreadoEn"] = "creado_en",
                ["ActualizadoEn"] = "actualizado_en"
            }
        );

        b.Entity<Usuario>()
            .Property(x => x.Rol)
            .HasConversion<string>();


        // =========================================================
        // PERMISOS
        // =========================================================

        b.Entity<Permiso>().ToTable("permisos");

        Map(
            b.Entity<Permiso>(),
            new()
            {
                ["Id"] = "id",
                ["Clave"] = "clave",
                ["Nombre"] = "nombre",
                ["Descripcion"] = "descripcion",
                ["Grupo"] = "grupo",
                ["Activo"] = "activo"
            }
        );


        // =========================================================
        // USUARIO - PERMISOS
        // =========================================================

        b.Entity<UsuarioPermiso>()
            .ToTable("usuario_permisos")
            .HasKey(x => new
            {
                x.UsuarioId,
                x.PermisoId
            });

        Map(
            b.Entity<UsuarioPermiso>(),
            new()
            {
                ["UsuarioId"] = "usuario_id",
                ["PermisoId"] = "permiso_id"
            }
        );


        // =========================================================
        // CATEGORIAS
        // =========================================================

        b.Entity<Categoria>().ToTable("categorias");

        Map(
            b.Entity<Categoria>(),
            new()
            {
                ["Id"] = "id",
                ["Nombre"] = "nombre",
                ["Descripcion"] = "descripcion",
                ["Activo"] = "activo",
                ["CreadoEn"] = "creado_en"
            }
        );


        // =========================================================
        // EQUIPOS
        // =========================================================

        b.Entity<Equipo>().ToTable("equipos");

        Map(
            b.Entity<Equipo>(),
            new()
            {
                ["Id"] = "id",
                ["Codigo"] = "codigo",
                ["Tipo"] = "tipo",
                ["Marca"] = "marca",
                ["Modelo"] = "modelo",
                ["NumeroSerie"] = "numero_serie",
                ["UsuarioId"] = "usuario_id",
                ["Ubicacion"] = "ubicacion",
                ["Estado"] = "estado",
                ["Observaciones"] = "observaciones",
                ["CreadoEn"] = "creado_en",
                ["ActualizadoEn"] = "actualizado_en"
            }
        );

        b.Entity<Equipo>()
            .Property(x => x.Estado)
            .HasConversion<string>();


        // =========================================================
        // SOLICITUDES
        // =========================================================

        b.Entity<Solicitud>().ToTable("solicitudes");

        Map(
            b.Entity<Solicitud>(),
            new()
            {
                ["Id"] = "id",
                ["Codigo"] = "codigo",
                ["UsuarioId"] = "usuario_id",
                ["CategoriaId"] = "categoria_id",
                ["EquipoId"] = "equipo_id",
                ["TecnicoId"] = "tecnico_id",
                ["TecnicoSolicitadoId"] = "tecnico_solicitado_id",
                ["Titulo"] = "titulo",
                ["Descripcion"] = "descripcion",
                ["Prioridad"] = "prioridad",
                ["Estado"] = "estado",
                ["PuedeContinuarTrabajando"] = "puede_continuar_trabajando",
                ["ModalidadAtencion"] = "modalidad_atencion",
                ["FechaVisitaSolicitada"] = "fecha_visita_solicitada",
                ["HoraVisitaSolicitada"] = "hora_visita_solicitada",
                ["FechaSolicitada"] = "fecha_solicitada",
                ["HoraInicioSolicitada"] = "hora_inicio_solicitada",
                ["HoraFinSolicitada"] = "hora_fin_solicitada",
                ["FechaProgramada"] = "fecha_programada",
                ["HoraInicioProgramada"] = "hora_inicio_programada",
                ["HoraFinProgramada"] = "hora_fin_programada",
                ["Solucion"] = "solucion",
                ["CreadoEn"] = "creado_en",
                ["ActualizadoEn"] = "actualizado_en",
                ["ResueltoEn"] = "resuelto_en",
                ["CerradoEn"] = "cerrado_en"
            }
        );

        b.Entity<Solicitud>()
            .Property(x => x.Prioridad)
            .HasConversion<string>();

        b.Entity<Solicitud>()
            .Property(x => x.Estado)
            .HasConversion<string>();

        b.Entity<Solicitud>()
            .Property(x => x.ModalidadAtencion)
            .HasConversion<string>();

        b.Entity<Solicitud>()
            .Property(x => x.FechaVisitaSolicitada)
            .HasColumnType("date");

        b.Entity<Solicitud>()
            .Property(x => x.FechaSolicitada)
            .HasColumnType("date");

        b.Entity<Solicitud>()
            .Property(x => x.FechaProgramada)
            .HasColumnType("date");


        // =========================================================
        // RELACIONES DE SOLICITUD
        // =========================================================

        // Usuario que creó la solicitud.
        // No se elimina la solicitud si se elimina el usuario.
        b.Entity<Solicitud>()
            .HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);


        // Técnico asignado.
        // Si el técnico se elimina, la solicitud conserva sus datos
        // pero el técnico queda como NULL.
        b.Entity<Solicitud>()
            .HasOne(x => x.Tecnico)
            .WithMany()
            .HasForeignKey(x => x.TecnicoId)
            .OnDelete(DeleteBehavior.SetNull);


        // Técnico solicitado por el usuario.
        // NO usamos SetNull aquí porque SQL Server detecta
        // múltiples rutas de eliminación en cascada.
        b.Entity<Solicitud>()
            .HasOne(x => x.TecnicoSolicitado)
            .WithMany()
            .HasForeignKey(x => x.TecnicoSolicitadoId)
            .OnDelete(DeleteBehavior.NoAction);


        // =========================================================
        // COMENTARIOS
        // =========================================================

        b.Entity<Comentario>().ToTable("comentarios");

        Map(
            b.Entity<Comentario>(),
            new()
            {
                ["Id"] = "id",
                ["SolicitudId"] = "solicitud_id",
                ["UsuarioId"] = "usuario_id",
                ["ComentarioTexto"] = "comentario",
                ["CreadoEn"] = "creado_en"
            }
        );


        // =========================================================
        // ADJUNTOS
        // =========================================================

        b.Entity<Adjunto>().ToTable("adjuntos");

        Map(
            b.Entity<Adjunto>(),
            new()
            {
                ["Id"] = "id",
                ["SolicitudId"] = "solicitud_id",
                ["UsuarioId"] = "usuario_id",
                ["NombreOriginal"] = "nombre_original",
                ["NombreArchivo"] = "nombre_archivo",
                ["Ruta"] = "ruta",
                ["TipoMime"] = "tipo_mime",
                ["Tamano"] = "tamano",
                ["CreadoEn"] = "creado_en"
            }
        );


        // =========================================================
        // MENSAJES PRIVADOS
        // =========================================================

        b.Entity<MensajePrivado>().ToTable("mensajes_privados");

        Map(b.Entity<MensajePrivado>(), new()
        {
            ["Id"] = "id",
            ["SolicitudId"] = "solicitud_id",
            ["RemitenteId"] = "remitente_id",
            ["DestinatarioId"] = "destinatario_id",
            ["AdjuntoId"] = "adjunto_id",
            ["Mensaje"] = "mensaje",
            ["LeidoEn"] = "leido_en",
            ["CreadoEn"] = "creado_en"
        });

        // Relación con la solicitud
        b.Entity<MensajePrivado>()
            .HasOne(x => x.Solicitud)
            .WithMany()
            .HasForeignKey(x => x.SolicitudId)
            .OnDelete(DeleteBehavior.NoAction);

        // Remitente
        b.Entity<MensajePrivado>()
            .HasOne(x => x.Remitente)
            .WithMany()
            .HasForeignKey(x => x.RemitenteId)
            .OnDelete(DeleteBehavior.NoAction);

        // Destinatario
        b.Entity<MensajePrivado>()
            .HasOne(x => x.Destinatario)
            .WithMany()
            .HasForeignKey(x => x.DestinatarioId)
            .OnDelete(DeleteBehavior.NoAction);

        // Adjunto opcional
        b.Entity<MensajePrivado>()
            .HasOne(x => x.Adjunto)
            .WithMany()
            .HasForeignKey(x => x.AdjuntoId)
            .OnDelete(DeleteBehavior.NoAction);


        // =========================================================
        // NOTIFICACIONES
        // =========================================================

        b.Entity<Notificacion>().ToTable("notificaciones");

        Map(
            b.Entity<Notificacion>(),
            new()
            {
                ["Id"] = "id",
                ["UsuarioId"] = "usuario_id",
                ["SolicitudId"] = "solicitud_id",
                ["Tipo"] = "tipo",
                ["Titulo"] = "titulo",
                ["Mensaje"] = "mensaje",
                ["Enlace"] = "enlace",
                ["LeidaEn"] = "leida_en",
                ["CreadoEn"] = "creado_en"
            }
        );


        // =========================================================
        // HISTORIAL DE SOLICITUDES
        // =========================================================

        b.Entity<HistorialSolicitud>().ToTable("historial_solicitudes");

        Map(
            b.Entity<HistorialSolicitud>(),
            new()
            {
                ["Id"] = "id",
                ["SolicitudId"] = "solicitud_id",
                ["UsuarioId"] = "usuario_id",
                ["EstadoAnterior"] = "estado_anterior",
                ["EstadoNuevo"] = "estado_nuevo",
                ["Descripcion"] = "descripcion",
                ["CreadoEn"] = "creado_en"
            }
        );
    }


    // =============================================================
    // MÉTODO PARA MAPEAR PROPIEDADES A NOMBRES DE SQL
    // =============================================================

    static void Map<T>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e,
        Dictionary<string, string> map
    )
        where T : class
    {
        foreach (var x in map)
        {
            e.Property(x.Key)
                .HasColumnName(x.Value);
        }
    }
}

