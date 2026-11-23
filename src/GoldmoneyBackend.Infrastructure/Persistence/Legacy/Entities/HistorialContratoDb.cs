namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

/// <summary>Tabla legacy HISTORIAL_CONTRATOS (copia del contrato cuando sale de CONTRATOS).</summary>
public sealed class HistorialContratoDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public int CodigoGrupo { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public string StatusHistorial { get; set; } = string.Empty;
    public string? IdCliente { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public decimal? CapitalPrestado { get; set; }
    public decimal? TasaInteres { get; set; }
    public decimal? InteresMensual { get; set; }
    public int? PlazoPago { get; set; }
    public DateTime? UltimaFechaPago { get; set; }
    public decimal? Saldo { get; set; }
    public decimal? SaldoCapital { get; set; }
    public string? UsuarioResponsable { get; set; }
    public DateTime? FechaIngreso { get; set; }
    public DateTime? HoraIngreso { get; set; }
    public string? UsuarioCreacion { get; set; }
    public DateTime? HoraCreacion { get; set; }
    public decimal? MontoMaximo { get; set; }
}
