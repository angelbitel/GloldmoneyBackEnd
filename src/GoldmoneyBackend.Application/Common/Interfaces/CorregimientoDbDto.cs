namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record CorregimientoDbDto(string CodigoCorregimiento, string CodigoDistrito, string? NombreCorregimiento, bool? Activo);
