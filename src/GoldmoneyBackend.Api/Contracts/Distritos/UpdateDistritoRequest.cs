namespace GoldmoneyBackend.Api.Contracts.Distritos;

public sealed record UpdateDistritoRequest(string CodigoProvincia, string? NombreDistrito, bool? Activo);
