using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;

public sealed class AbrirSesionCommandHandler : IRequestHandler<AbrirSesionCommand, SesionAbiertaDto>
{
    private readonly ISesionesDataService _sesionesDataService;

    public AbrirSesionCommandHandler(ISesionesDataService sesionesDataService)
    {
        _sesionesDataService = sesionesDataService;
    }

    public Task<SesionAbiertaDto> Handle(AbrirSesionCommand request, CancellationToken cancellationToken)
    {
        var dto = new AbrirSesionDto(request.CodigoEmpresa, request.FechaApertura, request.UsuarioResponsable);
        return _sesionesDataService.AbrirSesionAsync(dto, cancellationToken);
    }
}
