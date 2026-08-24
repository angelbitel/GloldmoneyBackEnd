using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Authentication;
using GoldmoneyBackend.Infrastructure.Authentication.Models;
using GoldmoneyBackend.Infrastructure.Persistence;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GoldmoneyBackend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var authSection = configuration.GetSection(AuthOptions.SectionName);

        services.Configure<JwtOptions>(options =>
        {
            options.Key = jwtSection["Key"] ?? string.Empty;
            options.Issuer = jwtSection["Issuer"] ?? string.Empty;
            options.Audience = jwtSection["Audience"] ?? string.Empty;
            options.ExpiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var minutes) ? minutes : 60;
        });

        services.Configure<AuthOptions>(options =>
        {
            var users = authSection.GetSection("Users").GetChildren();
            foreach (var user in users)
            {
                options.Users.Add(new AuthUser
                {
                    UserName = user["UserName"] ?? string.Empty,
                    Password = user["Password"] ?? string.Empty,
                    Roles = user.GetSection("Roles").GetChildren().Select(x => x.Value ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
                });
            }
        });

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no configurada.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddDbContext<LegacyDataDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IClienteReadRepository, ClienteReadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IClientesDataService, ClientesDataService>();
        services.AddScoped<IEmpresasDataService, EmpresasDataService>();
        services.AddScoped<IEmpeniosDataService, EmpeniosDataService>();
        services.AddScoped<IEmpeniosReadService, EmpeniosReadService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}