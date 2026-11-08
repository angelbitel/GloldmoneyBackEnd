namespace GoldmoneyBackend.Api.Contracts.Sesiones;

/// <summary>Empresa cuya caja se abre; la fecha de la sesión la pone el servidor.</summary>
public sealed record AbrirSesionRequest(string CodigoEmpresa);
