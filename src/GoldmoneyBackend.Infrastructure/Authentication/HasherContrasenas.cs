using System.Security.Cryptography;
using System.Text;
using GoldmoneyBackend.Infrastructure.Authentication.Models;
using Microsoft.Extensions.Options;

namespace GoldmoneyBackend.Infrastructure.Authentication;

/// <summary>
/// Hash de las contraseñas del sistema nuevo: PBKDF2-SHA256 con sal aleatoria y, si está configurado,
/// pepper (HMAC-SHA256 con una clave que vive fuera de la base).
/// Formato: <c>pbkdf2-sha256${iteraciones}${p|n}${sal}${hash}</c>, donde p/n indica si se usó pepper.
/// </summary>
public sealed class HasherContrasenas
{
    private const string Prefijo = "pbkdf2-sha256";
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    private readonly int _iteraciones;
    private readonly byte[]? _pepper;

    public HasherContrasenas(IOptions<ContrasenasOptions> opciones)
    {
        var valor = opciones.Value;

        if (valor.Iteraciones < ContrasenasOptions.IteracionesMinimas)
        {
            throw new InvalidOperationException(
                $"{ContrasenasOptions.SectionName}:Iteraciones debe ser al menos {ContrasenasOptions.IteracionesMinimas}.");
        }

        if (!string.IsNullOrEmpty(valor.Pepper) && valor.Pepper.Length < ContrasenasOptions.LongitudMinimaPepper)
        {
            throw new InvalidOperationException(
                $"{ContrasenasOptions.SectionName}:Pepper debe tener al menos {ContrasenasOptions.LongitudMinimaPepper} caracteres.");
        }

        _iteraciones = valor.Iteraciones;
        _pepper = string.IsNullOrEmpty(valor.Pepper) ? null : Encoding.UTF8.GetBytes(valor.Pepper);
    }

    public bool UsaPepper => _pepper is not null;

    public string Hash(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var hash = Derivar(contrasena, sal, _iteraciones, UsaPepper);

        return $"{Prefijo}${_iteraciones}${(UsaPepper ? 'p' : 'n')}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string contrasena, string almacenado)
    {
        if (!TryLeer(almacenado, out var iteraciones, out var conPepper, out var sal, out var esperado))
        {
            return false;
        }

        // Guardado con pepper pero hoy no hay ninguno configurado: no se puede comprobar.
        if (conPepper && _pepper is null)
        {
            return false;
        }

        var calculado = Derivar(contrasena, sal, iteraciones, conPepper);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    /// <summary>True si el hash se hizo con menos iteraciones que las configuradas o sin el pepper actual.</summary>
    public bool NecesitaRehacer(string almacenado)
    {
        return !TryLeer(almacenado, out var iteraciones, out var conPepper, out _, out _)
            || iteraciones < _iteraciones
            || conPepper != UsaPepper;
    }

    private byte[] Derivar(string contrasena, byte[] sal, int iteraciones, bool conPepper)
    {
        var entrada = Encoding.UTF8.GetBytes(contrasena);
        if (conPepper)
        {
            entrada = HMACSHA256.HashData(_pepper!, entrada);
        }

        return Rfc2898DeriveBytes.Pbkdf2(entrada, sal, iteraciones, HashAlgorithmName.SHA256, TamanoHash);
    }

    private static bool TryLeer(string almacenado, out int iteraciones, out bool conPepper, out byte[] sal, out byte[] hash)
    {
        iteraciones = 0;
        conPepper = false;
        sal = [];
        hash = [];

        var partes = almacenado.Split('$');
        if (partes.Length != 5
            || partes[0] != Prefijo
            || !int.TryParse(partes[1], out iteraciones)
            || iteraciones <= 0
            || partes[2] is not ("p" or "n"))
        {
            return false;
        }

        conPepper = partes[2] == "p";

        try
        {
            sal = Convert.FromBase64String(partes[3]);
            hash = Convert.FromBase64String(partes[4]);
            return hash.Length == TamanoHash;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
