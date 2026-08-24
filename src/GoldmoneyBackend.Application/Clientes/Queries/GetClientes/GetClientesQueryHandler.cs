using GoldmoneyBackend.Application.Clientes.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Queries.GetClientes;

public sealed class GetClientesQueryHandler : IRequestHandler<GetClientesQuery, IReadOnlyList<ClienteDto>>
{
    private readonly IClienteReadRepository _clienteReadRepository;

    public GetClientesQueryHandler(IClienteReadRepository clienteReadRepository)
    {
        _clienteReadRepository = clienteReadRepository;
    }

    public Task<IReadOnlyList<ClienteDto>> Handle(GetClientesQuery request, CancellationToken cancellationToken)
    {
        return _clienteReadRepository.GetAllAsync(cancellationToken);
    }
}