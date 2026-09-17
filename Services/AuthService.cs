using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SolicitudIT_Isertec.Data;
using SolicitudIT_Isertec.Models;

namespace SolicitudIT_Isertec.Services;

public class AuthService
{
    private readonly AppDbContext db;

    public AuthService(AppDbContext db)
        => this.db = db;

    public async Task<Usuario?> ValidateAsync(
        string login,
        string password)
    {
        var u = await db.Usuarios
            .SingleOrDefaultAsync(x =>
                x.UsuarioLogin == login ||
                x.Email == login);

        if (u is null ||
            !u.Activo ||
            string.IsNullOrEmpty(u.Password) ||
            !BCrypt.Net.BCrypt.Verify(password, u.Password))
        {
            return null;
        }

        return u;
    }

    public Task<bool> HasUsersAsync()
        => db.Usuarios.AnyAsync();

    public async Task CreateAdminAsync(
        string nombre,
        string apellido,
        string login,
        string email,
        string password)
    {
        var existe = await db.Usuarios.AnyAsync(x =>
            x.UsuarioLogin == login ||
            x.Email == email);

        if (existe)
            return;

        db.Usuarios.Add(new Usuario
        {
            Nombre = nombre,
            Apellido = apellido,
            UsuarioLogin = login,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = Rol.admin,
            Activo = true
        });

        await db.SaveChangesAsync();
    }

    public static ClaimsPrincipal Principal(Usuario u)
    {
        var identity = new ClaimsIdentity("AppCookie");

        identity.AddClaim(new Claim(
            ClaimTypes.NameIdentifier,
            u.Id.ToString()));

        identity.AddClaim(new Claim(
            ClaimTypes.Name,
            $"{u.Nombre} {u.Apellido}"));

        identity.AddClaim(new Claim(
            ClaimTypes.Email,
            u.Email));

        identity.AddClaim(new Claim(
            ClaimTypes.Role,
            u.Rol.ToString()));

        return new ClaimsPrincipal(identity);
    }
}