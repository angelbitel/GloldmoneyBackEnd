using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;

public sealed class GetContratoCompletoQueryHandler : IRequestHandler<GetContratoCompletoQuery, ContratoCompletoDto?>
{
    private readonly IEmpeniosReadService _empeniosReadService;

    public GetContratoCompletoQueryHandler(IEmpeniosReadService empeniosReadService)
    {
        _empeniosReadService = empeniosReadService;
    }

    public Task<ContratoCompletoDto?> Handle(GetContratoCompletoQuery request, CancellationToken cancellationToken)
    {
        return _empeniosReadService.GetContratoCompletoByIdAsync(request.ContratoId, cancellationToken);
    }
}