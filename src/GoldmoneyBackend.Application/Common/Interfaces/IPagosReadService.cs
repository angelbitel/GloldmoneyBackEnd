using GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>
/// Servicio de lectura de pagos pendientes sobre las tablas legacy (CONTRATOS, PAGOS).
/// </summary>
public interface IPagosReadService
{
    /// <summary>Devuelve los pagos pendientes y el total a pagar de un contrato, o null si no existe.</summary>
    Task<PagosPendientesDto?> GetPagosPendientesAsync(string contratoId, CancellationToken cancellationToken);
}