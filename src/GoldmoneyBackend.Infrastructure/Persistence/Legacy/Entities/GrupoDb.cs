namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class GrupoDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public int CodigoGrupo { get; set; }
    public string? AbreviaturaGrupo { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? DescripcionGrupo { get; set; }
    public decimal? TasaInteres { get; set; }
    public decimal? MesesPlazo { get; set; }
    public int? EstatusSerie { get; set; }
    public decimal? SerieInicial { get; set; }
    public int? EstadoGrupo { get; set; }
    public int? CaracteristicaGrupo { get; set; }
    public int? BloquearInteres { get; set; }
    public int? BloquearPlazo { get; set; }
    public decimal? TasaInteresNocturna { get; set; }
}
