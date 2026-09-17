namespace GoldmoneyBackend.Api.Contracts.Corregimientos;

public sealed record UpdateCorregimientoRequest(string CodigoDistrito, string? NombreCorregimiento, bool? Activo);
