using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;

public sealed class GetContratoByIdQueryHandler : IRequestHandler<GetContratoByIdQuery, ContratoDto>
{
    private readonly IEmpeniosReadService _empeniosReadService;

    public GetContratoByIdQueryHandler(IEmpeniosReadService empeniosReadService)
    {
        _empeniosReadService = empeniosReadService;
    }

    public async Task<ContratoDto> Handle(GetContratoByIdQuery request, CancellationToken cancellationToken)
    {
        var contrato = await _empeniosReadService.GetContratoByIdAsync(request.ContratoId, cancellationToken);

        if (contrato is null)
        {
            throw new NotFoundDomainException("Contrato no encontrado.");
        }

        return contrato;
    }
}
