namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class ContratoDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public int CodigoGrupo { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public string IdCliente { get; set; } = string.Empty;
    public string? Serie { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public decimal? CapitalPrestado { get; set; }
    public decimal? Interes { get; set; }
    public decimal? SaldoActual { get; set; }
    public decimal? InteresMensual { get; set; }
    public string? Observacion { get; set; }
    public DateTime? UltimaFechaPago { get; set; }
    public decimal? SaldoCapital { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public int? PlazoPago { get; set; }
    public string? UsuarioResponsable { get; set; }
    public DateTime? HoraTransaccion { get; set; }
    public decimal? MontoMaximo { get; set; }
}