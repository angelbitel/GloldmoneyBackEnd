namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>Apertura de caja de una empresa. La fecha la pone el servidor (fecha local de la empresa).</summary>
public sealed record AbrirSesionDto(
    string CodigoEmpresa,
    string UsuarioResponsable);
