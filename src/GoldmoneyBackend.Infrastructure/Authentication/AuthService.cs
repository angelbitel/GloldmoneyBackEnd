using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Data.Common;
using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Authentication.Models;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GoldmoneyBackend.Infrastructure.Authentication;

public sealed class AuthService : IAuthService
{
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

    public AuthService(
        IOptions<JwtOptions> jwtOptions,
        IOptions<AuthOptions> authOptions,
        LegacyDataDbContext legacyDataDbContext)
    {
        _jwtOptions = jwtOptions.Value;
        _authOptions = authOptions.Value;
        _legacyDataDbContext = legacyDataDbContext;
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

        var validationInDatabase = await TryValidateAgainstUsersTableAsync(userName, password, cancellationToken);

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

        var roles = configuredUser?.Roles ?? ["Analyst"];

        var nowUtc = DateTime.UtcNow;
        var expiresAtUtc = nowUtc.AddMinutes(_jwtOptions.ExpiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userName),
            new(JwtRegisteredClaimNames.UniqueName, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return new AuthTokenDto(accessToken, expiresAtUtc, userName, roles);
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
                        var verified = PasswordHasher.Verify(password, result.StoredPassword!);

                        if (verified && !PasswordHasher.IsPbkdf2Hash(result.StoredPassword!))
                        {
                            await TryUpgradeStoredPasswordHashAsync(
                                connection,
                                result.Table,
                                result.UserColumn,
                                result.PasswordColumn,
                                userName,
                                result.StoredPassword!,
                                cancellationToken);
                        }

                        return verified;
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

    private static async Task TryUpgradeStoredPasswordHashAsync(
        DbConnection connection,
        string table,
        string userColumn,
        string passwordColumn,
        string userName,
        string plainPassword,
        CancellationToken cancellationToken)
    {
        var upgradedPassword = PasswordHasher.Hash(plainPassword);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            UPDATE [{table}]
            SET [{passwordColumn}] = @upgradedPassword
            WHERE LTRIM(RTRIM([{userColumn}])) = @userName
              AND [{passwordColumn}] = @currentPassword
            """;

        var upgradedPasswordParameter = command.CreateParameter();
        upgradedPasswordParameter.ParameterName = "@upgradedPassword";
        upgradedPasswordParameter.Value = upgradedPassword;
        command.Parameters.Add(upgradedPasswordParameter);

        var userNameParameter = command.CreateParameter();
        userNameParameter.ParameterName = "@userName";
        userNameParameter.Value = userName.Trim();
        command.Parameters.Add(userNameParameter);

        var currentPasswordParameter = command.CreateParameter();
        currentPasswordParameter.ParameterName = "@currentPassword";
        currentPasswordParameter.Value = plainPassword;
        command.Parameters.Add(currentPasswordParameter);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (DbException)
        {
            // Non-fatal: user can still login even if migration update fails.
        }
    }
}
