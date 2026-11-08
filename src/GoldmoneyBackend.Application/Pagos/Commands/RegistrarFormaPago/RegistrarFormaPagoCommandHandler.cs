using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarFormaPago;

public sealed class RegistrarFormaPagoCommandHandler : IRequestHandler<RegistrarFormaPagoCommand, CobroDto>
{
    private readonly ICobrosRepository _cobros;

    public RegistrarFormaPagoCommandHandler(ICobrosRepository cobros)
    {
        _cobros = cobros;
    }

    public Task<CobroDto> Handle(RegistrarFormaPagoCommand request, CancellationToken cancellationToken)
    {
        var registro = new RegistroCobroDto(
            request.IdCliente.Trim(),
            request.UsuarioResponsable,
            DateTime.UtcNow,
            request.Metodos,
            request.Documentos
                .Select(d => d with { CodigoEmpresa = d.CodigoEmpresa.Trim(), NumeroContrato = d.NumeroContrato.Trim() })
                .ToList());

        return _cobros.RegistrarAsync(registro, cancellationToken);
    }
}
