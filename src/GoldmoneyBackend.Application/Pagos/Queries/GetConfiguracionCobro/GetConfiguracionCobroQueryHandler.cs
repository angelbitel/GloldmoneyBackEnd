using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetConfiguracionCobro;

public sealed class GetConfiguracionCobroQueryHandler : IRequestHandler<GetConfiguracionCobroQuery, ConfiguracionCobroDto>
{
    private readonly IPagosReadService _pagosReadService;

    public GetConfiguracionCobroQueryHandler(IPagosReadService pagosReadService)
    {
        _pagosReadService = pagosReadService;
    }

    public async Task<ConfiguracionCobroDto> Handle(GetConfiguracionCobroQuery request, CancellationToken cancellationToken)
    {
        return await _pagosReadService.GetConfiguracionCobroAsync(request.CodigoEmpresa.Trim(), cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, el sistema no pudo encontrar la empresa solicitada.");
    }
}
