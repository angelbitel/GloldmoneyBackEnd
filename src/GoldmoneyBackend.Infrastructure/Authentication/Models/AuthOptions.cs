namespace GoldmoneyBackend.Infrastructure.Authentication.Models;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public List<AuthUser> Users { get; init; } = [];
}
