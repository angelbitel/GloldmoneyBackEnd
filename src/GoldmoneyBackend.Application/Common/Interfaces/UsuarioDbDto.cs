namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record UsuarioDbDto(
    string Modulo,
    string NombreUsuario,
    string? NombreCompleto,
    decimal? StatusCuenta,
    int? IniciarDia,
    int? AplicarDescuento,
    int? UsuarioAdmin,
    int? DatosRetroactivos,
    int? EfectuarAnulacion,
    int? AccesoCashDrawer,
    int? UsuarioSoporte);
