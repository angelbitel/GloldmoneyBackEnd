using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Commands.ActualizarSaldos;

/// <summary>
/// SADE "Actualizar saldos": recalcula saldo_actual e interes_mensual de los contratos que usan la caja.
/// Con <see cref="Aplicar"/> = false solo devuelve el impacto (vista previa) sin modificar datos.
/// </summary>
public sealed record ActualizarSaldosCommand(string CodigoEmpresaCaja, bool Aplicar) : IRequest<ActualizacionSaldosDto>;

public sealed class ActualizarSaldosCommandHandler : IRequestHandler<ActualizarSaldosCommand, ActualizacionSaldosDto>
{
    private readonly ISesionesRepository _sesiones;

    public ActualizarSaldosCommandHandler(ISesionesRepository sesiones) => _sesiones = sesiones;

    public Task<ActualizacionSaldosDto> Handle(ActualizarSaldosCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoEmpresaCaja))
            throw new DomainValidationException("Estimado usuario, no ha seleccionado la empresa.");

        return _sesiones.ActualizarSaldosAsync(request.CodigoEmpresaCaja, request.Aplicar, cancellationToken);
    }
}
