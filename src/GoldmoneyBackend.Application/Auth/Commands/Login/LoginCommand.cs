using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Application.Common.Behaviors;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.Login;

public sealed record LoginCommand(string UserName, string Password) : IRequest<AuthTokenDto>, IRequestConDatosSensibles;
