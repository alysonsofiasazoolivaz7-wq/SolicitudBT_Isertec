# Instalación V4 en Visual Studio 2026

1. Instala Visual Studio 2026 y marca **ASP.NET y desarrollo web**.
2. Instala **.NET 10 SDK** y SQL Server.
3. Descomprime el ZIP y abre `SolicitudIT_Isertec.csproj`.
4. Cambia `DefaultConnection` en `appsettings.json` según tu instancia SQL Server.
5. Para base nueva: ejecuta `dotnet restore` y luego inicia el proyecto. La estructura de la base se crea con EF Core Migrations. En la Consola del Administrador de paquetes ejecuta `Add-Migration InitialCreate` y después `Update-Database`.
6. Entra a `/Account/Setup` para crear el primer administrador. Esa pantalla se bloquea automáticamente cuando ya existe un usuario.
7. Después crea usuarios BT y usuarios normales desde Administración.

## Base existente PHP
No uses `EnsureCreated` para esta V4. Las migraciones EF Core son el mecanismo oficial para crear y versionar el esquema SQL Server. No apuntes la aplicación a una base MySQL ni a una base SQL Server con un esquema incompatible sin realizar primero la migración de datos.

## Producción
La V4 utiliza migraciones EF Core versionadas. Para producción, publica detrás de IIS con HTTPS, mantén copias de seguridad y considera crear un usuario SQL específico para la aplicación en lugar de `sa`. Mantén `Uploads` fuera del repositorio y configura copias de seguridad.
