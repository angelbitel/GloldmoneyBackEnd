namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record DistritoDbUpsertDto(string CodigoDistrito, string CodigoProvincia, string? NombreDistrito, bool? Activo);
