namespace GoldmoneyBackend.Api.Contracts.Grupos;

public sealed record UpdateGrupoRequest(
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
