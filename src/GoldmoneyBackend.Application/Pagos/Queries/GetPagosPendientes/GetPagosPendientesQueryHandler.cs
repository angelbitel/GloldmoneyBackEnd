using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

public sealed class GetPagosPendientesQueryHandler : IRequestHandler<GetPagosPendientesQuery, PagosPendientesDto>
{
    private readonly IPagosReadService _pagosReadService;

    public GetPagosPendientesQueryHandler(IPagosReadService pagosReadService)
    {
        _pagosReadService = pagosReadService;
    }

    public async Task<PagosPendientesDto> Handle(GetPagosPendientesQuery request, CancellationToken cancellationToken)
    {
        // Equivale al status_proceso = 2 del SP sp_ad_abono_interes (contrato no encontrado).
        var pagos = await _pagosReadService.GetPagosPendientesAsync(request.ContratoId, cancellationToken);

        if (pagos is null)
        {
            throw new NotFoundDomainException("Contrato no encontrado.");
        }

        return pagos;
    }
}