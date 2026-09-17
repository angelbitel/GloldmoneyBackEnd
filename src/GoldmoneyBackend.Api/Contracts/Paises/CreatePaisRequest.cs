namespace GoldmoneyBackend.Api.Contracts.Paises;

public sealed record CreatePaisRequest(string CodigoPais, string? NombrePais, bool? Activo);
