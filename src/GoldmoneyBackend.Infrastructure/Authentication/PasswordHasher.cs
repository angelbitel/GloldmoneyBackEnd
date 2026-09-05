using System.Security.Cryptography;
using System.Text;

namespace GoldmoneyBackend.Infrastructure.Authentication;

internal static class PasswordHasher
{
    private const string Prefix = "pbkdf2";
    private const string Algorithm = "sha256";
    private const int DefaultIterations = 100000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static bool IsPbkdf2Hash(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith(Prefix + "$", StringComparison.OrdinalIgnoreCase);
    }

    public static string Hash(string password)
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, DefaultIterations, HashAlgorithmName.SHA256);
        var hash = deriveBytes.GetBytes(HashSize);

        return $"{Prefix}${Algorithm}${DefaultIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string providedPassword, string storedPassword)
    {
        if (TryVerifyPbkdf2(providedPassword, storedPassword, out var verified))
        {
            return verified;
        }

        return SecureEquals(storedPassword, providedPassword);
    }

    private static bool TryVerifyPbkdf2(string providedPassword, string storedPassword, out bool verified)
    {
        verified = false;

        if (string.IsNullOrWhiteSpace(storedPassword) || !storedPassword.StartsWith(Prefix + "$", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = storedPassword.Split('$', StringSplitOptions.None);
        if (parts.Length != 5)
        {
            return false;
        }

        if (!parts[1].Equals(Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[2], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expectedHash = Convert.FromBase64String(parts[4]);

            using var deriveBytes = new Rfc2898DeriveBytes(providedPassword, salt, iterations, HashAlgorithmName.SHA256);
            var actualHash = deriveBytes.GetBytes(expectedHash.Length);

            verified = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool SecureEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}