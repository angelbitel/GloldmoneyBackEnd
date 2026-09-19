namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;

public sealed record ContratoDetalleDto(
    int SecuenciaContratos,
    string? Descripcion,
    decimal? Kilates,
    decimal? Peso,
    int? CantidadProducto,
    int CodigoCategoriaPrenda,
    string? NombreCategoriaPrenda);