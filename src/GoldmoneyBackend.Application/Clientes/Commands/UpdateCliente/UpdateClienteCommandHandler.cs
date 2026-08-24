using GoldmoneyBackend.Application.Clientes.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Domain.ValueObjects;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Commands.UpdateCliente;

public sealed class UpdateClienteCommandHandler : IRequestHandler<UpdateClienteCommand, ClienteDto>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClienteCommandHandler(IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClienteDto> Handle(UpdateClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await _clienteRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundDomainException("Cliente no encontrado.");

        var email = Email.Create(request.Email);
        var documento = Documento.Create(request.Documento);

        if (await _clienteRepository.ExistsByEmailAsync(email.Value, request.Id, cancellationToken))
        {
            throw new ConflictDomainException("Ya existe un cliente con el mismo email.");
        }

        if (await _clienteRepository.ExistsByDocumentoAsync(documento.Value, request.Id, cancellationToken))
        {
            throw new ConflictDomainException("Ya existe un cliente con el mismo documento.");
        }

        cliente.Update(request.Nombre, email, documento);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return cliente.ToDto();
    }
}