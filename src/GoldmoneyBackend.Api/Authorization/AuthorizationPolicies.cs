namespace GoldmoneyBackend.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string Backoffice = "Backoffice";
    public const string ClientesRead = "ClientesRead";
    public const string ClientesWrite = "ClientesWrite";
    public const string ClientesDelete = "ClientesDelete";
}
