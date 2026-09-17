namespace GoldmoneyBackend.Api.Contracts.Sesiones;

public sealed record AbrirSesionRequest(string CodigoEmpresa, DateTime FechaApertura);
