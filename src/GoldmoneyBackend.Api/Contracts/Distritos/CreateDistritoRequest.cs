namespace GoldmoneyBackend.Api.Contracts.Distritos;

public sealed record CreateDistritoRequest(string CodigoDistrito, string CodigoProvincia, string? NombreDistrito, bool? Activo);
