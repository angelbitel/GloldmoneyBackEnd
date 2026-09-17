namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class ProvinciaDb
{
    public string CodigoProvincia { get; set; } = string.Empty;
    public string? NombreProvincia { get; set; }
    public bool? Activo { get; set; }
}
