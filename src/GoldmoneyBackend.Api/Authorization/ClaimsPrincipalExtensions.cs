using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GoldmoneyBackend.Api.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Usuario del token, que se graba como usuario responsable de la operación.</summary>
    public static string? UsuarioResponsable(this ClaimsPrincipal user)
    {
        var usuario = user.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(ClaimTypes.Name);

        return string.IsNullOrWhiteSpace(usuario) ? null : usuario;
    }
}
