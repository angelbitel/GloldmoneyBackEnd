using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenDto>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthTokenDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var token = await _authService.LoginAsync(request.UserName, request.Password, cancellationToken);

        if (token is null)
        {
            throw new UnauthorizedAccessException("Credenciales invalidas.");
        }

        return token;
    }
}
