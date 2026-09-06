namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class UsuarioDb
{
    public string Modulo { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string? NombreCompleto { get; set; }
    public string Contrasena { get; set; } = string.Empty;
    public decimal? StatusCuenta { get; set; }
    public int? IniciarDia { get; set; }
    public int? AplicarDescuento { get; set; }
    public int? UsuarioAdmin { get; set; }
    public int? DatosRetroactivos { get; set; }
    public int? EfectuarAnulacion { get; set; }
    public int? AccesoCashDrawer { get; set; }
    public int? UsuarioSoporte { get; set; }
}
