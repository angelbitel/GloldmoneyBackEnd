namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class DetalleContratoDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public int CodigoGrupo { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public int SecuenciaContratos { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Kilates { get; set; }
    public decimal? Peso { get; set; }
    public int? CodigoReloj { get; set; }
    public int? CantidadProducto { get; set; }
    public int CodigoCategoriaPrenda { get; set; }
}