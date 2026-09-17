namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record DistritoDbDto(string CodigoDistrito, string CodigoProvincia, string? NombreDistrito, bool? Activo);
