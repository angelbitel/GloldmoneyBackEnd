namespace GoldmoneyBackend.Infrastructure.Authentication.Models;

public sealed class AuthUser
{
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = [];
}
