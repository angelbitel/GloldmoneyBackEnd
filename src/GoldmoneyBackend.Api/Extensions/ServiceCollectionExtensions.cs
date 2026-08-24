using System.Text;
using GoldmoneyBackend.Api.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace GoldmoneyBackend.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        var jwtKey = configuration["Jwt:Key"] ?? "ChangeThisInProductionAndUseEnvVar";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "GoldmoneyBackend";
        var jwtAudience = configuration["Jwt:Audience"] ?? "GoldmoneyBackendClients";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(AuthorizationPolicies.Backoffice, policy =>
                policy.RequireRole("Admin", "Manager", "Analyst"));

            options.AddPolicy(AuthorizationPolicies.ClientesRead, policy =>
                policy.RequireRole("Admin", "Manager", "Analyst"));

            options.AddPolicy(AuthorizationPolicies.ClientesWrite, policy =>
                policy.RequireRole("Admin", "Manager"));

            options.AddPolicy(AuthorizationPolicies.ClientesDelete, policy =>
                policy.RequireRole("Admin"));
        });

        return services;
    }
}