namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

/// <summary>
/// Registro de la tabla legacy PAGOS (abono de interés) usado por el
/// procedimiento almacenado sp_ad_abono_interes.
/// </summary>
public sealed class PagoDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public int NumeroPagos { get; set; }
    public int CodigoGrupo { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public int CantidadPagos { get; set; }
    public decimal PagoInteres { get; set; }
    public decimal PagoCapital { get; set; }
    public decimal SaldoCapital { get; set; }
    public decimal SaldoInteres { get; set; }
    public DateTime UltimaFechaPago { get; set; }
    public string UsuarioResponsable { get; set; } = string.Empty;
    public int SecuenciaRecibo { get; set; }
    public DateTime HoraTransaccion { get; set; }
}