namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record GrupoDbDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string? AbreviaturaGrupo,
    DateTime? FechaCreacion,
    string? DescripcionGrupo,
    decimal? TasaInteres,
    decimal? MesesPlazo,
    int? EstatusSerie,
    decimal? SerieInicial,
    int? EstadoGrupo,
    int? CaracteristicaGrupo,
    int? BloquearInteres,
    int? BloquearPlazo,
    decimal? TasaInteresNocturna);
