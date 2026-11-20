using GoldmoneyBackend.Application.Common.Behaviors;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.CambiarMiPin;

/// <summary>El usuario conectado define su propio PIN, confirmando con su contraseña.</summary>
public sealed record CambiarMiPinCommand(string NombreUsuario, string Contrasena, string Pin) : IRequest, IRequestConDatosSensibles;
