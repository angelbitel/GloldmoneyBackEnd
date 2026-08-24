using GoldmoneyBackend.Application.Clientes.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Queries.GetClienteById;

public sealed class GetClienteByIdQueryHandler : IRequestHandler<GetClienteByIdQuery, ClienteDto>
{
    private readonly IClienteReadRepository _clienteReadRepository;

    public GetClienteByIdQueryHandler(IClienteReadRepository clienteReadRepository)
    {
        _clienteReadRepository = clienteReadRepository;
    }

    public async Task<ClienteDto> Handle(GetClienteByIdQuery request, CancellationToken cancellationToken)
    {
        return await _clienteReadRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundDomainException("Cliente no encontrado.");
    }
}