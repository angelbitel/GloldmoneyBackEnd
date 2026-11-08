using GoldmoneyBackend.Application.Common.Behaviors;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.CambiarContrasenaUsuario;

/// <summary>Un administrador asigna a un usuario su contraseña del sistema nuevo; la del VB6 no cambia.</summary>
public sealed record CambiarContrasenaUsuarioCommand(string NombreUsuario, string NuevaContrasena) : IRequest, IRequestConDatosSensibles;
