namespace GoldmoneyBackend.Application.Auth.DTOs;

public sealed record AuthTokenDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string UserName,
    IReadOnlyList<string> Roles);
