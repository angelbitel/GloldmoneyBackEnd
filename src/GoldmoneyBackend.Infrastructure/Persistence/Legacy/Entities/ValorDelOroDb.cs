namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class ValorDelOroDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public string StatusCalidad { get; set; } = string.Empty;
    public decimal Kilataje { get; set; }
    public decimal? MaximoValor { get; set; }
    public decimal? MinimoValor { get; set; }
}
