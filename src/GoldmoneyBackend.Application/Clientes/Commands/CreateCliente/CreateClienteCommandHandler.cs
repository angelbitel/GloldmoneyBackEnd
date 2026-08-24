using GoldmoneyBackend.Application.Clientes.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Domain.Entities.Clientes;
using GoldmoneyBackend.Domain.ValueObjects;
using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Commands.CreateCliente;

public sealed class CreateClienteCommandHandler : IRequestHandler<CreateClienteCommand, ClienteDto>
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClienteCommandHandler(IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClienteDto> Handle(CreateClienteCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        var documento = Documento.Create(request.Documento);

        if (await _clienteRepository.ExistsByEmailAsync(email.Value, null, cancellationToken))
        {
            throw new ConflictDomainException("Ya existe un cliente con el mismo email.");
        }

        if (await _clienteRepository.ExistsByDocumentoAsync(documento.Value, null, cancellationToken))
        {
            throw new ConflictDomainException("Ya existe un cliente con el mismo documento.");
        }

        var cliente = Cliente.Create(request.Nombre, email, documento);

        await _clienteRepository.AddAsync(cliente, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return cliente.ToDto();
    }
}