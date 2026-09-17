namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class IniciarCierreSesionDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public DateTime FechaApertura { get; set; }
    public DateTime? HoraCierre { get; set; }
    public string? UsuarioApertura { get; set; }
    public string? UsuarioCierre { get; set; }
    public decimal? ValorInicialCaja { get; set; }
    public decimal? ValorFinalCaja { get; set; }
    public decimal? StatusSesion { get; set; }
}
