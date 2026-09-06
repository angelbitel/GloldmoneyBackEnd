namespace GoldmoneyBackend.Api.Contracts.Usuarios;

public sealed record UpdateUsuarioRequest(
    string Modulo,
    string NombreUsuario,
    string? NombreCompleto,
    string Contrasena,
    decimal? StatusCuenta,
    int? IniciarDia,
    int? AplicarDescuento,
    int? UsuarioAdmin,
    int? DatosRetroactivos,
    int? EfectuarAnulacion,
    int? AccesoCashDrawer,
    int? UsuarioSoporte);
