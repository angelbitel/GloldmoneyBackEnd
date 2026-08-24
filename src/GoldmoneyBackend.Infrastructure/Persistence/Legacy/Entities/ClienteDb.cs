namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class ClienteDb
{
    public string IdCliente { get; set; } = string.Empty;
    public string? Apellido { get; set; }
    public string? Nombre { get; set; }
    public string? Telefono { get; set; }
    public int? Estatus { get; set; }
    public string? Direccion { get; set; }
    public string? Comentario { get; set; }
    public string? CodigoPais { get; set; }
    public string? CodigoProvincia { get; set; }
    public string? CodigoDistrito { get; set; }
    public string? CodigoCorregimiento { get; set; }
}
