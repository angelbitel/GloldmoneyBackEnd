using GoldmoneyBackend.Application.Pagos;
using GoldmoneyBackend.Application.Pagos.Queries.GetConfiguracionCobro;

namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>
/// Lectura de contratos activos (tabla legacy CONTRATOS) con el contexto de su empresa
/// (fecha de sesión y días de gracia) para calcular cobros.
/// </summary>
public interface IPagosReadService
{
    /// <summary>Devuelve un contrato por su identificador compuesto o número, o null si no existe.</summary>
    Task<ContratoPagoDto?> GetContratoAsync(string contratoId, CancellationToken cancellationToken);

    /// <summary>Devuelve los contratos activos de un cliente, ordenados por número de contrato.</summary>
    Task<IReadOnlyList<ContratoPagoDto>> GetContratosClienteAsync(string idCliente, CancellationToken cancellationToken);

    /// <summary>Datos de impresión y parámetros de cobro de la empresa, o null si no existe.</summary>
    Task<ConfiguracionCobroDto?> GetConfiguracionCobroAsync(string codigoEmpresa, CancellationToken cancellationToken);
}
