using System.Data.Common;
using FluentAssertions;
using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Infrastructure.Authentication;
using GoldmoneyBackend.Infrastructure.Authentication.Models;
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
                    Password = "pbkdf2$sha256$100000$l5CBIqgbe8AhcmfSmSb02w==$2aTCzjyVz96cgRXAv+kooKkbiatcqGPqNx6OuXHH+50=",
                    Roles = ["Admin", "Manager", "Analyst"]
                }
            ]
        });

        var dbContext = CreateBrokenDbContext();
        var sut = new AuthService(options, authOptions, dbContext);

        var result = await sut.LoginAsync("copilot", "Copilot123!", CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserName.Should().Be("copilot");
        result.Roles.Should().Contain("Admin");
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    private static LegacyDataDbContext CreateBrokenDbContext()
    {
        var options = new DbContextOptionsBuilder<LegacyDataDbContext>()
            .UseSqlServer("Server=localhost;Database=does-not-exist;User Id=sa;Password=invalid;TrustServerCertificate=True;Encrypt=False;")
            .Options;

        return new LegacyDataDbContext(options);
    }
}
