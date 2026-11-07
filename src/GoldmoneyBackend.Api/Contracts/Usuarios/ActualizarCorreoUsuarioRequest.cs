namespace GoldmoneyBackend.Api.Contracts.Usuarios;

/// <param name="Correo">Correo del usuario; vacío o null para eliminarlo.</param>
public sealed record ActualizarCorreoUsuarioRequest(string? Correo);
