namespace GoldmoneyBackend.Api.Contracts.Sesiones;

/// <summary>Empresa cuya caja se abre y PIN del usuario; la fecha de la sesión la pone el servidor.</summary>
public sealed record AbrirSesionRequest(string CodigoEmpresa, string Pin);
