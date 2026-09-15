namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record CategoriaPrendaDbDto(
    int CodigoCategoriaPrenda,
    string? NombreCategoriaPrenda,
    bool? Activo);
