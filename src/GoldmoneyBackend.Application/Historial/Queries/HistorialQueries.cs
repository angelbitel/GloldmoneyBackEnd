using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Historial.Queries;

public sealed record GetPagosContratoQuery(string CodigoEmpresa, int CodigoGrupo, string NumeroContrato)
    : IRequest<IReadOnlyList<PagoHistorialDto>>;

public sealed record GetHistorialClienteQuery(string Cedula) : IRequest<HistorialClienteDto>;

public sealed record GetMetricasCarteraQuery(string CodigoEmpresa, string? Cedula) : IRequest<MetricasCarteraDto>;

public sealed class GetPagosContratoQueryHandler(IHistorialReadService service)
    : IRequestHandler<GetPagosContratoQuery, IReadOnlyList<PagoHistorialDto>>
{
    public Task<IReadOnlyList<PagoHistorialDto>> Handle(GetPagosContratoQuery request, CancellationToken cancellationToken) =>
        service.GetPagosContratoAsync(request.CodigoEmpresa, request.CodigoGrupo, request.NumeroContrato, cancellationToken);
}

public sealed class GetHistorialClienteQueryHandler(IHistorialReadService service)
    : IRequestHandler<GetHistorialClienteQuery, HistorialClienteDto>
{
    public Task<HistorialClienteDto> Handle(GetHistorialClienteQuery request, CancellationToken cancellationToken) =>
        service.GetHistorialClienteAsync(request.Cedula, cancellationToken);
}

public sealed class GetMetricasCarteraQueryHandler(IHistorialReadService service)
    : IRequestHandler<GetMetricasCarteraQuery, MetricasCarteraDto>
{
    public Task<MetricasCarteraDto> Handle(GetMetricasCarteraQuery request, CancellationToken cancellationToken) =>
        service.GetMetricasAsync(request.CodigoEmpresa, request.Cedula, cancellationToken);
}
