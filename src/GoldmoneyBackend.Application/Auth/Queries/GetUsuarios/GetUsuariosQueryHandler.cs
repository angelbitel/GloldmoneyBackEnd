using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Queries.GetUsuarios;

public sealed class GetUsuariosQueryHandler : IRequestHandler<GetUsuariosQuery, IReadOnlyList<UsuarioDto>>
{
    private readonly IAuthService _authService;

    public GetUsuariosQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IReadOnlyList<UsuarioDto>> Handle(GetUsuariosQuery request, CancellationToken cancellationToken)
    {
        return await _authService.GetUsuariosAsync(cancellationToken);
    }
}
