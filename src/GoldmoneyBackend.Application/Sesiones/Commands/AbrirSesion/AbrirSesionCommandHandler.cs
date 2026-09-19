using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;

public sealed class AbrirSesionCommandHandler : IRequestHandler<AbrirSesionCommand, SesionAbiertaDto>
{
    private readonly ISesionesRepository _sesionesRepository;

    public AbrirSesionCommandHandler(ISesionesRepository sesionesRepository)
    {
        _sesionesRepository = sesionesRepository;
    }

    public Task<SesionAbiertaDto> Handle(AbrirSesionCommand request, CancellationToken cancellationToken)
    {
        var dto = new AbrirSesionDto(request.CodigoEmpresa, request.FechaApertura, request.UsuarioResponsable);
        return _sesionesRepository.AbrirSesionAsync(dto, cancellationToken);
    }
}
