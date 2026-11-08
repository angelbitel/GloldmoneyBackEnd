using System.Data.Common;
using System.Security.Cryptography;
using FluentAssertions;
using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Infrastructure.Authentication;
using GoldmoneyBackend.Infrastructure.Authentication.Models;
using GoldmoneyBackend.Infrastructure.Persistence;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GoldmoneyBackend.Application.Tests.Auth;

public sealed class AuthServiceFallbackTests
{
    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenLegacyDatabaseIsUnavailable_AndUserExistsInAppSettings()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "0123456789abcdef0123456789abcdef",
            Issuer = "GoldmoneyBackend",
            Audience = "GoldmoneyBackendClients",
            ExpiresMinutes = 60
        });

        var authOptions = Options.Create(new AuthOptions
        {
            Users =
            [
                new AuthUser
                {
                    UserName = "copilot",
                    Password = HashDeConfiguracion("Copilot123!"),
                    Roles = ["Admin", "Manager", "Analyst"]
                }
            ]
        });

        var sut = new AuthService(
            options,
            authOptions,
            CreateBrokenDbContext<LegacyDataDbContext>(o => new LegacyDataDbContext(o)),
            CreateBrokenDbContext<ApplicationDbContext>(o => new ApplicationDbContext(o)),
            new HasherContrasenas(Options.Create(new ContrasenasOptions())));

        var result = await sut.LoginAsync("copilot", "Copilot123!", CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserName.Should().Be("copilot");
        result.Roles.Should().Contain("Admin");
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>Hash en el formato de Auth:Users (pbkdf2$sha256$iteraciones$sal$hash).</summary>
    private static string HashDeConfiguracion(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, 100_000, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2$sha256$100000${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    private static TContext CreateBrokenDbContext<TContext>(Func<DbContextOptions<TContext>, TContext> crear)
        where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseSqlServer("Server=localhost;Database=does-not-exist;User Id=sa;Password=invalid;TrustServerCertificate=True;Encrypt=False;")
            .Options;

        return crear(options);
    }
}
