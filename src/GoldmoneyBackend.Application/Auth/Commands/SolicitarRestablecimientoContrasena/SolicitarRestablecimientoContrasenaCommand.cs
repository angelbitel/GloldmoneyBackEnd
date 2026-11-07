using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.SolicitarRestablecimientoContrasena;

/// <summary>
/// Pide un enlace para restablecer la contraseña. No revela si el usuario existe:
/// el resultado es el mismo haya o no una cuenta con correo registrado.
/// </summary>
public sealed record SolicitarRestablecimientoContrasenaCommand(string UsuarioOCorreo) : IRequest;
