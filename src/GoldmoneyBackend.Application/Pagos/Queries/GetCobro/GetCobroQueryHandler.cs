using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetCobro;

public sealed class GetCobroQueryHandler : IRequestHandler<GetCobroQuery, CobroDto>
{
    private readonly ICobrosRepository _cobros;

    public GetCobroQueryHandler(ICobrosRepository cobros)
    {
        _cobros = cobros;
    }

    public async Task<CobroDto> Handle(GetCobroQuery request, CancellationToken cancellationToken)
    {
        return await _cobros.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundDomainException("Cobro no encontrado.");
    }
}
