using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Queries.GetEstadoSesiones;

public sealed class GetEstadoSesionesQueryHandler : IRequestHandler<GetEstadoSesionesQuery, IReadOnlyList<EstadoSesionEmpresaDto>>
{
    private readonly ISesionesDataService _sesionesDataService;

    public GetEstadoSesionesQueryHandler(ISesionesDataService sesionesDataService)
    {
        _sesionesDataService = sesionesDataService;
    }

    public Task<IReadOnlyList<EstadoSesionEmpresaDto>> Handle(GetEstadoSesionesQuery request, CancellationToken cancellationToken)
    {
        return _sesionesDataService.GetEstadoSesionesAsync(cancellationToken);
    }
}
