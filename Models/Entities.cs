using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SolicitudIT_Isertec.Models;

public enum Rol { usuario, tecnico, admin }
public enum Prioridad { baja, media, alta, critica }
public enum EstadoSolicitud { pendiente, asignada, en_proceso, esperando_usuario, resuelta, cerrada }
public enum ModalidadAtencion { presencial, remoto }
public enum EstadoEquipo { activo, mantenimiento, baja }

public class Usuario {
 [Key] public int Id {get;set;} [Required,MaxLength(100)] public string Nombre {get;set;}=""; [Required,MaxLength(100)] public string Apellido {get;set;}="";
 [Required,MaxLength(50)] public string UsuarioLogin {get;set;}=""; [Required,MaxLength(150)] public string Email {get;set;}=""; [Required,MaxLength(255)] public string Password {get;set;}="";
 public Rol Rol {get;set;}=Rol.usuario; public bool Activo {get;set;}=true; public DateTime CreadoEn {get;set;}=DateTime.UtcNow; public DateTime ActualizadoEn {get;set;}=DateTime.UtcNow;
 public ICollection<Equipo> Equipos {get;set;}=new List<Equipo>();
}
public class Permiso { [Key] public int Id {get;set;} [Required,MaxLength(80)] public string Clave {get;set;}=""; [Required,MaxLength(120)] public string Nombre {get;set;}=""; [MaxLength(255)] public string Descripcion {get;set;}=""; [MaxLength(60)] public string Grupo {get;set;}=""; public bool Activo {get;set;}=true; }
public class UsuarioPermiso { public int UsuarioId {get;set;} public int PermisoId {get;set;} public Usuario Usuario {get;set;}=null!; public Permiso Permiso {get;set;}=null!; }
public class Categoria { [Key] public int Id {get;set;} [Required,MaxLength(100)] public string Nombre {get;set;}=""; [MaxLength(255)] public string? Descripcion {get;set;} public bool Activo {get;set;}=true; public DateTime CreadoEn {get;set;}=DateTime.UtcNow; }
public class Equipo { [Key] public int Id {get;set;} [Required,MaxLength(50)] public string Codigo {get;set;}=""; [Required,MaxLength(80)] public string Tipo {get;set;}=""; [MaxLength(80)] public string? Marca {get;set;} [MaxLength(100)] public string? Modelo {get;set;} [MaxLength(150)] public string? NumeroSerie {get;set;} public int? UsuarioId {get;set;} public Usuario? Usuario {get;set;} [MaxLength(150)] public string? Ubicacion {get;set;} public EstadoEquipo Estado {get;set;}=EstadoEquipo.activo; public string? Observaciones {get;set;} public DateTime CreadoEn {get;set;}=DateTime.UtcNow; public DateTime ActualizadoEn {get;set;}=DateTime.UtcNow; }
public class Solicitud {
 [Key] public int Id {get;set;} [Required,MaxLength(20)] public string Codigo {get;set;}=""; public int UsuarioId {get;set;} public Usuario Usuario {get;set;}=null!; public int CategoriaId {get;set;} public Categoria Categoria {get;set;}=null!; public int? EquipoId {get;set;} public Equipo? Equipo {get;set;}
 public int? TecnicoId {get;set;} public Usuario? Tecnico {get;set;} public int? TecnicoSolicitadoId {get;set;} public Usuario? TecnicoSolicitado {get;set;}
 [Required,MaxLength(180)] public string Titulo {get;set;}=""; [Required] public string Descripcion {get;set;}=""; public Prioridad Prioridad {get;set;}=Prioridad.media; public EstadoSolicitud Estado {get;set;}=EstadoSolicitud.pendiente; public bool PuedeContinuarTrabajando {get;set;}=true; public ModalidadAtencion ModalidadAtencion {get;set;}=ModalidadAtencion.remoto;
 public DateOnly? FechaVisitaSolicitada {get;set;} public TimeSpan? HoraVisitaSolicitada {get;set;} public DateOnly? FechaSolicitada {get;set;} public TimeSpan? HoraInicioSolicitada {get;set;} public TimeSpan? HoraFinSolicitada {get;set;} public DateOnly? FechaProgramada {get;set;} public TimeSpan? HoraInicioProgramada {get;set;} public TimeSpan? HoraFinProgramada {get;set;}
 public string? Solucion {get;set;} public DateTime CreadoEn {get;set;}=DateTime.UtcNow; public DateTime ActualizadoEn {get;set;}=DateTime.UtcNow; public DateTime? ResueltoEn {get;set;} public DateTime? CerradoEn {get;set;}
 public ICollection<Comentario> Comentarios {get;set;}=new List<Comentario>(); public ICollection<Adjunto> Adjuntos {get;set;}=new List<Adjunto>(); public ICollection<MensajePrivado> Mensajes {get;set;}=new List<MensajePrivado>();
}
public class Comentario { [Key] public int Id {get;set;} public int SolicitudId {get;set;} public Solicitud Solicitud {get;set;}=null!; public int UsuarioId {get;set;} public Usuario Usuario {get;set;}=null!; [Required] public string ComentarioTexto {get;set;}=""; public DateTime CreadoEn {get;set;}=DateTime.UtcNow; }
public class Adjunto { [Key] public int Id {get;set;} public int SolicitudId {get;set;} public Solicitud Solicitud {get;set;}=null!; public int UsuarioId {get;set;} public Usuario Usuario {get;set;}=null!; [Required,MaxLength(255)] public string NombreOriginal {get;set;}=""; [Required,MaxLength(255)] public string NombreArchivo {get;set;}=""; [Required,MaxLength(500)] public string Ruta {get;set;}=""; [MaxLength(100)] public string? TipoMime {get;set;} public long Tamano {get;set;} public DateTime CreadoEn {get;set;}=DateTime.UtcNow; }
public class MensajePrivado { [Key] public int Id {get;set;} public int SolicitudId {get;set;} public Solicitud Solicitud {get;set;}=null!; public int RemitenteId {get;set;} public Usuario Remitente {get;set;}=null!; public int DestinatarioId {get;set;} public Usuario Destinatario {get;set;}=null!; public int? AdjuntoId {get;set;} public Adjunto? Adjunto {get;set;} [Required] public string Mensaje {get;set;}=""; public DateTime? LeidoEn {get;set;} public DateTime CreadoEn {get;set;}=DateTime.UtcNow; }
public class Notificacion { [Key] public int Id {get;set;} public int UsuarioId {get;set;} public Usuario Usuario {get;set;}=null!; public int? SolicitudId {get;set;} public Solicitud? Solicitud {get;set;} [Required,MaxLength(50)] public string Tipo {get;set;}=""; [Required,MaxLength(180)] public string Titulo {get;set;}=""; [Required,MaxLength(500)] public string Mensaje {get;set;}=""; [MaxLength(255)] public string? Enlace {get;set;} public DateTime? LeidaEn {get;set;} public DateTime CreadoEn {get;set;}=DateTime.UtcNow; }
public class HistorialSolicitud { [Key] public int Id {get;set;} public int SolicitudId {get;set;} public Solicitud Solicitud {get;set;}=null!; public int UsuarioId {get;set;} public Usuario Usuario {get;set;}=null!; [MaxLength(30)] public string? EstadoAnterior {get;set;} [MaxLength(30)] public string? EstadoNuevo {get;set;} [Required,MaxLength(255)] public string Descripcion {get;set;}=""; public DateTime CreadoEn {get;set;}=DateTime.UtcNow; }
