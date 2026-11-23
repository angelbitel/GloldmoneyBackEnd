namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

/// <summary>Tabla legacy COPIA_RECIBO (copias de recibo cobradas por contrato).</summary>
public sealed class CopiaReciboDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public int NumeroCopia { get; set; }
    public int CodigoGrupo { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public string? IdCliente { get; set; }
    public DateTime? Fecha { get; set; }
    public DateTime? Hora { get; set; }
    public decimal? Capital { get; set; }
    public string? UsuarioResponsable { get; set; }
    public int? SecuenciaRecibo { get; set; }
}
