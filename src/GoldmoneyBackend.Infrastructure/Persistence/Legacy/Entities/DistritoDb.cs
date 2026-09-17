namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class DistritoDb
{
    public string CodigoDistrito { get; set; } = string.Empty;
    public string CodigoProvincia { get; set; } = string.Empty;
    public string? NombreDistrito { get; set; }
    public bool? Activo { get; set; }
}
