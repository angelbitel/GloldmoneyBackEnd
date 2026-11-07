using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.RestablecerContrasena;

/// <summary>Cambia la contraseña usando el token recibido por correo.</summary>
public sealed record RestablecerContrasenaCommand(string Token, string NuevaContrasena) : IRequest;
