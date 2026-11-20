namespace GoldmoneyBackend.Api.Authorization;

public static class RateLimitPolicies
{
    /// <summary>Endpoints públicos de recuperación de contraseña: límite por IP.</summary>
    public const string RecuperacionContrasena = "RecuperacionContrasena";

    /// <summary>Endpoints que comprueban el PIN o la contraseña del usuario conectado: límite por usuario.</summary>
    public const string VerificacionPin = "VerificacionPin";
}
