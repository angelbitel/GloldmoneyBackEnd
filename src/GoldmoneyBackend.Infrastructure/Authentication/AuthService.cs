using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Data.Common;
using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Authentication.Models;
using GoldmoneyBackend.Infrastructure.Persistence;
using GoldmoneyBackend.Infrastructure.Persistence.Cuentas;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GoldmoneyBackend.Infrastructure.Authentication;

public sealed class AuthService : IAuthService
{
    private const string DefaultSigningKey = "GoldmoneyBackend-Development-Key-At-Least-32-Characters";

    private static readonly string[] CandidateTables =
    [
        "usuarios",
        "usuario",
        "tbl_usuarios",
        "tbl_usuario",
        "users",
        "user"
    ];

    private static readonly string[] CandidateUserColumns =
    [
        "nombre_usuario",
        "usuario",
        "username",
        "user_name",
        "login",
        "nombre"
    ];

    private static readonly string[] CandidatePasswordColumns =
    [
        "clave",
        "password",
        "contrasena",
        "contrasenia",
        "passwd",
        "pass"
    ];

    private readonly JwtOptions _jwtOptions;
    private readonly AuthOptions _authOptions;
    private readonly LegacyDataDbContext _legacyDataDbContext;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly HasherContrasenas _hasher;

    public AuthService(
        IOptions<JwtOptions> jwtOptions,
        IOptions<AuthOptions> authOptions,
        LegacyDataDbContext legacyDataDbContext,
        ApplicationDbContext applicationDbContext,
        HasherContrasenas hasher)
    {
        _jwtOptions = jwtOptions.Value;
        _authOptions = authOptions.Value;
        _legacyDataDbContext = legacyDataDbContext;
        _applicationDbContext = applicationDbContext;
        _hasher = hasher;
    }

    public Task<IReadOnlyList<UsuarioDto>> GetUsuariosAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Nunca se devuelve la contrasena ni su hash: solo identificacion y roles.
        IReadOnlyList<UsuarioDto> usuarios = _authOptions.Users
            .Select(u => new UsuarioDto(u.UserName, u.Roles))
            .ToList();

        return Task.FromResult(usuarios);
    }

    public async Task<AuthTokenDto?> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool? validationInDatabase;

        try
        {
            // Con contraseña propia del sistema nuevo solo vale esa; si no, la de USUARIOS.
            validationInDatabase = await TryValidateAgainstCredencialAsync(userName, password, cancellationToken)
                ?? await TryValidateAgainstUsersTableAsync(userName, password, cancellationToken);
        }
        catch (DbException)
        {
            validationInDatabase = null;
        }

        if (validationInDatabase == false)
        {
            return null;
        }

        if (validationInDatabase is null)
        {
            var fallbackUser = _authOptions.Users.FirstOrDefault(u =>
                string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));

            if (fallbackUser is null || !PasswordHasher.Verify(password, fallbackUser.Password))
            {
                return null;
            }
        }

        var configuredUser = _authOptions.Users.FirstOrDefault(u =>
            string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));

        var roles = configuredUser?.Roles
            ?? _authOptions.RolesUsuarios.GetValueOrDefault(userName.Trim())
            ?? ["Analyst"];

        var nowUtc = DateTime.UtcNow;
        var expiresAtUtc = nowUtc.AddMinutes(_jwtOptions.ExpiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userName),
            new(JwtRegisteredClaimNames.UniqueName, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKeyValue = string.IsNullOrWhiteSpace(_jwtOptions.Key)
            ? DefaultSigningKey
            : _jwtOptions.Key;
        var issuer = string.IsNullOrWhiteSpace(_jwtOptions.Issuer) ? "GoldmoneyBackend" : _jwtOptions.Issuer;
        var audience = string.IsNullOrWhiteSpace(_jwtOptions.Audience) ? "GoldmoneyBackendClients" : _jwtOptions.Audience;

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyValue));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return new AuthTokenDto(accessToken, expiresAtUtc, userName, roles);
    }

    /// <summary>
    /// Valida contra UsuariosCredencial. Null si el usuario no tiene contraseña del sistema nuevo
    /// o la tabla no está disponible, para seguir con el resto de las validaciones.
    /// </summary>
    private async Task<bool?> TryValidateAgainstCredencialAsync(string userName, string password, CancellationToken cancellationToken)
    {
        var nombre = userName.Trim();
        CredencialUsuario? credencial;

        try
        {
            credencial = await _applicationDbContext.UsuariosCredencial
                .FirstOrDefaultAsync(x => x.NombreUsuario == nombre, cancellationToken);
        }
        // EF envuelve los errores de conexión transitorios en InvalidOperationException.
        catch (Exception ex) when (ex is DbException || ex.InnerException is DbException)
        {
            return null;
        }

        if (credencial is null)
        {
            return null;
        }

        if (!_hasher.Verificar(password, credencial.Hash))
        {
            return false;
        }

        // Si el usuario se eliminó en el VB6, su contraseña del sistema nuevo deja de servir.
        if (!await _legacyDataDbContext.Usuarios.AsNoTracking().AnyAsync(x => x.NombreUsuario == nombre, cancellationToken))
        {
            return false;
        }

        if (_hasher.NecesitaRehacer(credencial.Hash))
        {
            credencial.Hash = _hasher.Hash(password);
            credencial.ActualizadoEnUtc = DateTime.UtcNow;
            await _applicationDbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private async Task<bool?> TryValidateAgainstUsersTableAsync(string userName, string password, CancellationToken cancellationToken)
    {
        var connection = _legacyDataDbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        foreach (var table in CandidateTables)
        {
            foreach (var userColumn in CandidateUserColumns)
            {
                foreach (var passwordColumn in CandidatePasswordColumns)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var result = await TryGetStoredPasswordAsync(
                        connection,
                        table,
                        userColumn,
                        passwordColumn,
                        userName,
                        cancellationToken);

                    if (result.Found)
                    {
                        // Solo lectura: USUARIOS.contrasena la comparte el VB6 y no admite otro formato.
                        return PasswordHasher.Verify(password, result.StoredPassword!);
                    }
                }
            }
        }

        // El usuario no existe en ninguna tabla candidata: se permite intentar el fallback de appsettings.
        return null;
    }

    private static async Task<(bool Executed, bool Found, string? StoredPassword, string Table, string UserColumn, string PasswordColumn)> TryGetStoredPasswordAsync(
        DbConnection connection,
        string table,
        string userColumn,
        string passwordColumn,
        string userName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT TOP 1 [{passwordColumn}]
            FROM [{table}]
            WHERE LTRIM(RTRIM([{userColumn}])) = @userName
            """;

        var userNameParameter = command.CreateParameter();
        userNameParameter.ParameterName = "@userName";
        userNameParameter.Value = userName.Trim();
        command.Parameters.Add(userNameParameter);

        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result is DBNull)
            {
                return (true, false, null, table, userColumn, passwordColumn);
            }

            return (true, true, result.ToString(), table, userColumn, passwordColumn);
        }
        catch (DbException)
        {
            return (false, false, null, table, userColumn, passwordColumn);
        }

    }
}
