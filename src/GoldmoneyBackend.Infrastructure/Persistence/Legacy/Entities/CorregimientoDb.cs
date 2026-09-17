namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class CorregimientoDb
{
    public string CodigoCorregimiento { get; set; } = string.Empty;
    public string CodigoDistrito { get; set; } = string.Empty;
    public string? NombreCorregimiento { get; set; }
    public bool? Activo { get; set; }
}
