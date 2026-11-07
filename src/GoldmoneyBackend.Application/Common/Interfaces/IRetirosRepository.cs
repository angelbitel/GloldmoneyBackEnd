using GoldmoneyBackend.Domain.Pagos;

namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>
/// Retiro de contratos sobre las tablas legacy, replicando <c>Salvar_Retiro_de_Contrato</c> del VB6:
/// RETIROS, HISTORIAL_CONTRATOS, HISTORIAL_PAGOS, H_COPIAS_RECIBO, H_DETALLES_CONTRATOS,
/// MOVIMIENTO_CAJA, MOVIMIENTO_TEMPORAL y EMPRESA, y el borrado del contrato original.
/// </summary>
public interface IRetirosRepository
{
    /// <summary>
    /// Ejecuta <paramref name="operacion"/> en una transacción propia. Si falla se revierte
    /// y se descartan los cambios pendientes, para que no afecten a la siguiente operación.
    /// </summary>
    Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken);

    /// <summary>Contrato activo bloqueado hasta el fin de la transacción, o null si no existe o ya fue retirado.</summary>
    Task<ContratoRetiroDto?> GetContratoBloqueadoAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken);

    /// <summary>Fecha de la sesión abierta de la empresa (null si no hay), días de gracia y parámetro de retiro.</summary>
    Task<SesionRetiroDto> GetSesionAbiertaAsync(string codigoEmpresa, CancellationToken cancellationToken);

    /// <summary>
    /// Graba el retiro ya calculado, pasa el contrato y sus pagos, copias y prendas al historial,
    /// actualiza la caja, ingresa los movimientos MR/IR y borra el contrato original. Devuelve el número de recibo.
    /// </summary>
    Task<int> RegistrarRetiroAsync(RegistroRetiroDto registro, CancellationToken cancellationToken);
}

/// <param name="Interes">Tasa mensual en porcentaje (<c>CONTRATOS.interes</c>).</param>
public sealed record ContratoRetiroDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    string IdCliente,
    DateTime? FechaCreacion,
    DateTime? UltimaFechaPago,
    DateTime? FechaVencimiento,
    decimal SaldoCapital,
    decimal Interes);

/// <param name="RestarAbonoCapital">Parámetro <c>restar_abono_capital</c> de la empresa.</param>
public sealed record SesionRetiroDto(DateTime? FechaSesion, int DiasGracia, bool RestarAbonoCapital);

public sealed record RegistroRetiroDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    string IdCliente,
    DateTime FechaSesion,
    string UsuarioResponsable,
    RetiroCalculado Retiro);
