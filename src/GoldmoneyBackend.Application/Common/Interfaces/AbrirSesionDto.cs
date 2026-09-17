namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record AbrirSesionDto(
    string CodigoEmpresa,
    DateTime FechaApertura,
    string UsuarioResponsable);
