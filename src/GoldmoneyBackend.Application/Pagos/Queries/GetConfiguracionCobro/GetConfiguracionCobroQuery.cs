using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetConfiguracionCobro;

/// <summary>Consulta los datos de impresión y los parámetros de cobro de una empresa.</summary>
public sealed record GetConfiguracionCobroQuery(string CodigoEmpresa) : IRequest<ConfiguracionCobroDto>;
