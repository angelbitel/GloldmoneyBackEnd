namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record PaisDbUpsertDto(string CodigoPais, string? NombrePais, bool? Activo);
