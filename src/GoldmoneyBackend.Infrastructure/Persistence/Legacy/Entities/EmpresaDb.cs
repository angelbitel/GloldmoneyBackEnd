namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class EmpresaDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public string? NombreEmpresa { get; set; }
    public string? Direccion { get; set; }
    public string? Ruc { get; set; }
    public string? Telefono { get; set; }
    public decimal? MontoInicial { get; set; }
    public decimal? MontoAuxiliar { get; set; }
    public int? ManejoCajaDep { get; set; }
    public int? CodEmpresaCaja { get; set; }
}
