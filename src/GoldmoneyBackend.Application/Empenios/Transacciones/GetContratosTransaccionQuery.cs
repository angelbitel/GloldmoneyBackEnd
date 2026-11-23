using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Transacciones;

public sealed record GetContratosTransaccionQuery(string Cedula) : IRequest<IReadOnlyList<ContratoTransaccionDto>>;

public sealed class GetContratosTransaccionQueryHandler
    : IRequestHandler<GetContratosTransaccionQuery, IReadOnlyList<ContratoTransaccionDto>>
{
    private readonly ITransaccionesContratoRepository _repository;

    public GetContratosTransaccionQueryHandler(ITransaccionesContratoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ContratoTransaccionDto>> Handle(GetContratosTransaccionQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Cedula))
            throw new DomainValidationException("Estimado usuario, no ha seleccionado un cliente.");

        var contratos = await _repository.GetContratosPorCedulaAsync(request.Cedula.Trim(), cancellationToken);
        if (contratos.Count == 0) return [];

        var ahora = await _repository.GetFechaServidorAsync(cancellationToken);

        // Parámetros y sesión se consultan una sola vez por empresa.
        var porEmpresa = new Dictionary<string, (ParametrosTransaccionDto? Parametros, SesionOperacionDto? Sesion)>();
        foreach (var codigoEmpresa in contratos.Select(c => c.CodigoEmpresa.Trim()).Distinct())
        {
            var parametros = await _repository.GetParametrosAsync(codigoEmpresa, cancellationToken);
            var sesion = await _repository.GetSesionOperacionAsync(codigoEmpresa, cancellationToken);
            porEmpresa[codigoEmpresa] = (parametros, sesion);
        }

        return contratos
            .Select(c =>
            {
                var (parametros, sesion) = porEmpresa[c.CodigoEmpresa.Trim()];
                var a = AdministracionContrato.Construir(c, parametros, sesion, ahora);

                return new ContratoTransaccionDto(
                    c.CodigoEmpresa.Trim(), a.NombreEmpresa, c.CodigoGrupo, a.AbreviaturaGrupo,
                    c.NumeroContrato.Trim(), c.Serie?.Trim(), c.IdCliente.Trim(), c.CapitalPrestado,
                    c.FechaCreacion, c.Interes, c.PlazoPago, c.FechaVencimiento, c.InteresMensual, c.SaldoCapital,
                    c.Observacion?.Trim(), c.HoraTransaccion, c.UsuarioResponsable?.Trim(),
                    a.Estado, a.CantidadCopias, a.TienePagos,
                    a.FechaSesion, a.CostoCopia,
                    a.PuedeAnular, a.MotivoNoAnulable, a.RequiereSupervisor,
                    c.Detalles);
            })
            .ToList();
    }
}
