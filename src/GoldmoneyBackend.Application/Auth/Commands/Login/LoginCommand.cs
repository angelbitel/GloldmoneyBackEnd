using GoldmoneyBackend.Application.Auth.DTOs;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.Login;

public sealed record LoginCommand(string UserName, string Password) : IRequest<AuthTokenDto>;
