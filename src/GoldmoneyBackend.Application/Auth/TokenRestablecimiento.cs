using System.Security.Cryptography;
using System.Text;

namespace GoldmoneyBackend.Application.Auth;

/// <summary>Token aleatorio de un solo uso para el enlace de restablecimiento de contraseña.</summary>
public static class TokenRestablecimiento
{
    private const int Bytes = 32;

    /// <summary>Token en claro, apto para URL (base64url, 43 caracteres).</summary>
    public static string Generar() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(Bytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    /// <summary>Hash SHA-256 en hexadecimal que se guarda en la base en lugar del token.</summary>
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
