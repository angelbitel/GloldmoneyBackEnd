using GoldmoneyBackend.Application.Empenios.Transacciones;
using GoldmoneyBackend.Application.Empenios.Vencidos;

namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>Retiro de contratos vencidos y su reversión (SADE: frmRetiroContratosVencidos).</summary>
public interface IContratosVencidosRepository
{
    Task<ContextoVencimiento> GetContextoAsync(string codigoEmpresa, CancellationToken cancellationToken);

    /// <summary>Contratos de la empresa/grupo filtrados por rango de fechas; los meses se calculan en Application.</summary>
    Task<IReadOnlyList<ContratoVencidoDatos>> GetContratosAsync(FiltroContratosVencidos filtro, CancellationToken cancellationToken);

    Task<ContratoVencidoDatos?> GetContratoAsync(ContratoClave clave, ModoVencidos modo, CancellationToken cancellationToken);

    Task<DetalleVencidoDatos> GetDetalleAsync(ContratoClave clave, ModoVencidos modo, CancellationToken cancellationToken);

    /// <summary>Pasa los contratos a HISTORIAL_CONTRATOS (CF) con prendas, pagos y copias, y los elimina de CONTRATOS. Todo o nada.</summary>
    Task RetirarAsync(RetiroContratosDatos datos, CancellationToken cancellationToken);

    /// <summary>Devuelve contratos CF a CONTRATOS con prendas, pagos y copias. Todo o nada.</summary>
    Task DeshacerRetiroAsync(IReadOnlyList<ContratoClave> contratos, CancellationToken cancellationToken);
}
