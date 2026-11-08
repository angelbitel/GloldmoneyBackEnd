namespace GoldmoneyBackend.Infrastructure.Authentication.Models;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public List<AuthUser> Users { get; init; } = [];

    /// <summary>
    /// Roles de usuarios de la tabla USUARIOS (VB6), por nombre sin distinguir mayúsculas.
    /// Quien no aparece aquí ni en <see cref="Users"/> entra como Analyst.
    /// </summary>
    public Dictionary<string, List<string>> RolesUsuarios { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
