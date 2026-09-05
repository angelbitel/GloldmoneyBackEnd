namespace GoldmoneyBackend.Application.Auth.DTOs;

public sealed record UsuarioDto(
    string UserName,
    IReadOnlyList<string> Roles);
