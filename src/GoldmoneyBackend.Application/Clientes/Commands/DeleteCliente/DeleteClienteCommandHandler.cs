using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Commands.DeleteCliente;

public sealed class DeleteClienteCommandHandler : IRequestHandler<DeleteClienteCommand>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteClienteCommandHandler(IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await _clienteRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundDomainException("Cliente no encontrado.");

        cliente.MarkAsDeleted();
        _clienteRepository.Remove(cliente);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}