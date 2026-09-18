# SolicitudIT Isertec V4

Migración completa de la aplicación de soporte BT a **ASP.NET Core 10 + SQL Server + Entity Framework Core 10**.

## Incluye
- Login con BCrypt compatible con hashes bcrypt de la versión PHP.
- Roles usuario / tecnico / admin.
- Solicitudes, categorías, equipos, prioridades y estados.
- Presencial/remoto, fecha y hora aproximada solicitada.
- Programación BT de fecha/hora de atención.
- Asignación o toma de tickets.
- Comentarios e historial.
- Adjuntos de imágenes/documentos.
- Notificaciones solamente para usuarios, destinadas a informar sobre respuestas y actualizaciones de sus solicitudes.
- Calendario BT.
- Administración de usuarios y equipos.
- UI gris/azul y logo BT.

## Abrir en Visual Studio
1. Instala Visual Studio 2026 con **ASP.NET y desarrollo web** y .NET 10 SDK.
2. Abre `SolicitudIT_Isertec.csproj`.
3. Edita `appsettings.json` con tu servidor SQL Server.
4. Abre la Consola del Administrador de paquetes y ejecuta `Add-Migration InitialCreate` y `Update-Database` si es una base nueva.
5. Ejecuta con el botón HTTPS.

> Para una base existente proveniente de MySQL/PHP, NO ejecutes `Update-Database` a ciegas. Primero alinea la estructura y migra los datos.

## Credenciales
No se incluye una contraseña administrativa fija. Crea el primer usuario admin desde SQL Server usando un hash BCrypt generado por la aplicación o adapta un pequeño bootstrap administrativo antes de producción.


## Base de datos V4
La V4 utiliza EF Core 10 Migrations para administrar el esquema de SQL Server. En Visual Studio: `Tools > NuGet Package Manager > Package Manager Console`, verifica `Default project` como `SolicitudIT_Isertec` y ejecuta:

```powershell
Add-Migration InitialCreate
Update-Database
```

El paquete `Microsoft.EntityFrameworkCore.Tools` ya está incluido en el proyecto.


## Ajustes pendientes / preparados
- Notificaciones por correo: configuración `Email` agregada en `appsettings.json`; queda pendiente conectar el proveedor/API cuando se obtengan credenciales.
- Exportación de reportes: administración puede exportar solicitudes en CSV y PDF.
- Inventario de dispositivos: los equipos se relacionan con `UsuarioId` (usuario/correo) y cada usuario solo puede seleccionar sus dispositivos asignados; la carga masiva queda pendiente de recibir el inventario real.
- Asignación de solicitudes: el usuario no elige técnico; BT puede aceptar solicitudes y administración puede asignarlas.
