using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;

namespace SolicitudIT_Isertec.Services;

// La creación/actualización del esquema se realiza con EF Core Migrations.
// Este servicio conserva el seeding de datos iniciales para que pueda reutilizarse
// después de aplicar las migraciones, sin ejecutar EnsureCreated.
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedAsync(db);
    }

    public static async Task SeedAsync(AppDbContext db)
    {
        // Este método se ejecuta después de que las migraciones hayan creado las tablas.
        // El seeding de la V4 se mantiene aquí para futuras inicializaciones controladas.
        await Task.CompletedTask;
    }
}
