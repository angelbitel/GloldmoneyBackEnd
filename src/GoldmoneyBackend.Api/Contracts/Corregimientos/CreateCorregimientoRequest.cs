namespace GoldmoneyBackend.Api.Contracts.Corregimientos;

public sealed record CreateCorregimientoRequest(string CodigoCorregimiento, string CodigoDistrito, string? NombreCorregimiento, bool? Activo);
