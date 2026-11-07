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
        var contrato = await _pagosReadService.GetContratoAsync(request.ContratoId, cancellationToken);

        if (contrato is null)
        {
            throw new NotFoundDomainException("Contrato no encontrado.");
        }

        return CalculadorCobro.CalcularPagosPendientes(contrato);
    }
}
