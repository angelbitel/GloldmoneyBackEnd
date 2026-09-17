namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class PaisDb
{
    public string CodigoPais { get; set; } = string.Empty;
    public string? NombrePais { get; set; }
    public bool? Activo { get; set; }
}
