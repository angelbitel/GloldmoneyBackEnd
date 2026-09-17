namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record CorregimientoDbUpsertDto(string CodigoCorregimiento, string CodigoDistrito, string? NombreCorregimiento, bool? Activo);
