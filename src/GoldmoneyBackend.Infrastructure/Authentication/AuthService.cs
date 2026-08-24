using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Authentication.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GoldmoneyBackend.Infrastructure.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly JwtOptions _jwtOptions;
    private readonly AuthOptions _authOptions;

    public AuthService(IOptions<JwtOptions> jwtOptions, IOptions<AuthOptions> authOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _authOptions = authOptions.Value;
    }

    public Task<AuthTokenDto?> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = _authOptions.Users.FirstOrDefault(u =>
            string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            return Task.FromResult<AuthTokenDto?>(null);
        }

        if (!SecureEquals(user.Password, password))
        {
            return Task.FromResult<AuthTokenDto?>(null);
        }

        var nowUtc = DateTime.UtcNow;
        var expiresAtUtc = nowUtc.AddMinutes(_jwtOptions.ExpiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserName),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

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

        var dto = new AuthTokenDto(accessToken, expiresAtUtc, user.UserName, user.Roles);
        return Task.FromResult<AuthTokenDto?>(dto);
    }

    private static bool SecureEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < left.Length; i++)
        {
            result |= left[i] ^ right[i];
        }

        return result == 0;
    }
}
