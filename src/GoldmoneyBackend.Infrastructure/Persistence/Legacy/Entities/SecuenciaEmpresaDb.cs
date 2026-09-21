namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class SecuenciaEmpresaDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public string CodigoSecuencia { get; set; } = string.Empty;
    public int SecuenciaActual { get; set; }
}