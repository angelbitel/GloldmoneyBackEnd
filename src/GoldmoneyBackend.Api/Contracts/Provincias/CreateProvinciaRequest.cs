namespace GoldmoneyBackend.Api.Contracts.Provincias;

public sealed record CreateProvinciaRequest(string CodigoProvincia, string? NombreProvincia, bool? Activo);
