using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;
using SolicitudIT_Isertec.Models;
using SolicitudIT_Isertec.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Autenticación
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "BT",
        policy => policy.RequireRole("tecnico", "admin")
    );

    options.AddPolicy(
        "Admin",
        policy => policy.RequireRole("admin")
    );
});

// Servicios
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();


// ==========================================================
// CREAR USUARIOS DE PRUEBA AUTOMÁTICAMENTE
// ==========================================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Solo crear usuarios si la tabla está completamente vacía.
    if (!await db.Usuarios.AnyAsync())
    {
        var administrador = new Usuario
        {
            Nombre = "Administrador",
            Apellido = "Sistema",
            UsuarioLogin = "admin",
            Email = "admin@isertec.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
            Rol = Rol.admin,
            Activo = true,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var tecnico = new Usuario
        {
            Nombre = "Tecnico",
            Apellido = "BT",
            UsuarioLogin = "tecnico",
            Email = "tecnico@isertec.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Tecnico123*"),
            Rol = Rol.tecnico,
            Activo = true,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        var usuario = new Usuario
        {
            Nombre = "Usuario",
            Apellido = "Prueba",
            UsuarioLogin = "usuario",
            Email = "usuario@isertec.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Usuario123*"),
            Rol = Rol.usuario,
            Activo = true,
            CreadoEn = DateTime.UtcNow,
            ActualizadoEn = DateTime.UtcNow
        };

        db.Usuarios.AddRange(
            administrador,
            tecnico,
            usuario
        );

        await db.SaveChangesAsync();
    }
}


// ==========================================================
// CONFIGURACIÓN DE LA APLICACIÓN
// ==========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();


// Ruta principal
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);


// ==========================================================
// INICIO
// ==========================================================

app.Run();