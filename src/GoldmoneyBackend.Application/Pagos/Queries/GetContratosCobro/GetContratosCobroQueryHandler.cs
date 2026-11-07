using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetContratosCobro;

public sealed class GetContratosCobroQueryHandler : IRequestHandler<GetContratosCobroQuery, IReadOnlyList<ContratoCobroDto>>
{
    private readonly IPagosReadService _pagosReadService;

    public GetContratosCobroQueryHandler(IPagosReadService pagosReadService)
    {
        _pagosReadService = pagosReadService;
    }

    public async Task<IReadOnlyList<ContratoCobroDto>> Handle(GetContratosCobroQuery request, CancellationToken cancellationToken)
    {
        var contratos = await _pagosReadService.GetContratosClienteAsync(request.IdCliente, cancellationToken);

        return contratos.Select(CalculadorCobro.ConstruirFila).ToList();
    }
}
