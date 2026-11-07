using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.ActualizarCorreoUsuario;

/// <summary>Registra o cambia el correo de un usuario. Un correo vacío lo elimina.</summary>
public sealed record ActualizarCorreoUsuarioCommand(string NombreUsuario, string? Correo) : IRequest;
