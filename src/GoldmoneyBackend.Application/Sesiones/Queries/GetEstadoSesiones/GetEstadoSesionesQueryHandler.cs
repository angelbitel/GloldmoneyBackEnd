using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Queries.GetEstadoSesiones;

public sealed class GetEstadoSesionesQueryHandler : IRequestHandler<GetEstadoSesionesQuery, IReadOnlyList<EstadoSesionEmpresaDto>>
{
    private readonly ISesionesRepository _sesionesRepository;

    public GetEstadoSesionesQueryHandler(ISesionesRepository sesionesRepository)
    {
        _sesionesRepository = sesionesRepository;
    }

    public Task<IReadOnlyList<EstadoSesionEmpresaDto>> Handle(GetEstadoSesionesQuery request, CancellationToken cancellationToken)
    {
        return _sesionesRepository.GetEstadoSesionesAsync(cancellationToken);
    }
}
