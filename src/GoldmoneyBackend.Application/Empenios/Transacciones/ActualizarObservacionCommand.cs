using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Transacciones;

public sealed record ActualizarObservacionCommand(ContratoClave Contrato, string? Observacion) : IRequest<string?>;

/// <summary>Equivale a SalvarObservacionContrato de SADE: UPDATE CONTRATOS SET observacion.</summary>
public sealed class ActualizarObservacionCommandHandler : IRequestHandler<ActualizarObservacionCommand, string?>
{
    private readonly ITransaccionesContratoRepository _repository;

    public ActualizarObservacionCommandHandler(ITransaccionesContratoRepository repository)
    {
        _repository = repository;
    }

    public async Task<string?> Handle(ActualizarObservacionCommand request, CancellationToken cancellationToken)
    {
        var observacion = ReglasTransaccionContrato.NormalizarObservacion(request.Observacion);

        var actualizado = await _repository.ActualizarObservacionAsync(request.Contrato.Normalizada(), observacion, cancellationToken);
        if (!actualizado)
            throw new NotFoundDomainException("Estimado usuario, el número de contrato no existe en la base de datos.");

        return observacion;
    }
}
