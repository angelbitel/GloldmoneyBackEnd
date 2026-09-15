using GoldmoneyBackend.Api.Extensions;
using GoldmoneyBackend.Api.Middleware;
using GoldmoneyBackend.Application;
using GoldmoneyBackend.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "FrontendCors";
var configuredUrls = builder.Configuration["ASPNETCORE_URLS"];
var hasHttpsEndpoint = !string.IsNullOrWhiteSpace(configuredUrls)
    && configuredUrls.Contains("https://", StringComparison.OrdinalIgnoreCase);
var corsAllowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? [];

var isAllowedOrigin = (string? origin) =>
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return false;
    }

    if (corsAllowedOrigins.Any(configuredOrigin =>
            string.Equals(configuredOrigin, origin, StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    try
    {
        var originUri = new Uri(origin);
        var host = originUri.Host;

        return host.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Contains("0.0.0.0", StringComparison.OrdinalIgnoreCase)
            || host.Contains(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.Contains(".vercel.app", StringComparison.OrdinalIgnoreCase)
            || host.Contains(".ngrok.io", StringComparison.OrdinalIgnoreCase)
            || host.Contains(".localtest.me", StringComparison.OrdinalIgnoreCase);
    }
    catch
    {
        return false;
    }
};

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policyBuilder =>
    {
        policyBuilder
            .SetIsOriginAllowed(isAllowedOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapScalarApiReference(options =>
{
    options.WithTitle("Goldmoney Backend API");
    options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
});

app.UseSwagger();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
if (hasHttpsEndpoint && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Ok(new { message = "Goldmoney Backend API is running", health = "/health", login = "/api/auth/login" }));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

public partial class Program;
