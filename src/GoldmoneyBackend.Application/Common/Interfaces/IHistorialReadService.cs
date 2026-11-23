using GoldmoneyBackend.Application.Historial;

namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IHistorialReadService
{
    /// <summary>Pagos de un contrato (activos e históricos), del más reciente al más antiguo.</summary>
    Task<IReadOnlyList<PagoHistorialDto>> GetPagosContratoAsync(
        string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken);

    /// <summary>Contratos de un cliente que ya están en HISTORIAL_CONTRATOS, separados por estado.</summary>
    Task<HistorialClienteDto> GetHistorialClienteAsync(string cedula, CancellationToken cancellationToken);

    /// <summary>Métricas de cartera de la empresa; si se indica cédula, solo de ese cliente.</summary>
    Task<MetricasCarteraDto> GetMetricasAsync(string codigoEmpresa, string? cedula, CancellationToken cancellationToken);
}
