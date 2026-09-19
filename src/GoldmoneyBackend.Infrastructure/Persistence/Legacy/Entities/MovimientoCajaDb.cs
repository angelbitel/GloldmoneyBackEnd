namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class MovimientoCajaDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public int NumeroMovimiento { get; set; }
    public string CodigoTransaccion { get; set; } = string.Empty;
    public int? CodigoGrupo { get; set; }
    public string? NumeroContrato { get; set; }
    public decimal? MontoTransaccion { get; set; }
    public DateTime? FechaTransaccion { get; set; }
    public DateTime? HoraTransaccion { get; set; }
    public string? MotivoTransaccion { get; set; }
    public string? UsuarioResponsable { get; set; }
}