using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratosByCedula;

public sealed class GetContratosByCedulaQueryHandler : IRequestHandler<GetContratosByCedulaQuery, IReadOnlyList<ContratoDto>>
{
    private readonly IEmpeniosReadService _empeniosReadService;

    public GetContratosByCedulaQueryHandler(IEmpeniosReadService empeniosReadService)
    {
        _empeniosReadService = empeniosReadService;
    }

    public async Task<IReadOnlyList<ContratoDto>> Handle(GetContratosByCedulaQuery request, CancellationToken cancellationToken)
    {
        return await _empeniosReadService.GetContratosByCedulaAsync(request.Cedula, cancellationToken);
    }
}
