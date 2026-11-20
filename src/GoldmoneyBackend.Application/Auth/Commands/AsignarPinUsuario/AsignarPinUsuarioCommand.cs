using GoldmoneyBackend.Application.Common.Behaviors;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.AsignarPinUsuario;

/// <summary>Un administrador asigna el PIN de un usuario (por ejemplo, si lo olvidó).</summary>
public sealed record AsignarPinUsuarioCommand(string NombreUsuario, string Pin) : IRequest, IRequestConDatosSensibles;
