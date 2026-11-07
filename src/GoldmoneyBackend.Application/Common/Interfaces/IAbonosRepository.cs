using GoldmoneyBackend.Domain.Pagos;

namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>
/// Escritura de abonos a interés/capital sobre las tablas legacy (PAGOS, CONTRATOS,
/// MOVIMIENTO_CAJA, MOVIMIENTO_TEMPORAL, EMPRESA), replicando <c>Salvar_Abono_de_Interes</c> del VB6.
/// </summary>
public interface IAbonosRepository
{
    /// <summary>
    /// Ejecuta <paramref name="operacion"/> en una transacción propia. Si falla se revierte
    /// y se descartan los cambios pendientes, para que no afecten a la siguiente operación.
    /// </summary>
    Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken);

    /// <summary>
    /// Contrato activo bloqueado para actualización hasta el fin de la transacción,
    /// o null si no existe (un contrato retirado ya no está en CONTRATOS).
    /// </summary>
    Task<ContratoAbonoDto?> GetContratoBloqueadoAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken);

    /// <summary>Fecha de la sesión abierta de la empresa (null si no hay) y sus días de gracia.</summary>
    Task<SesionCobroDto> GetSesionAbiertaAsync(string codigoEmpresa, CancellationToken cancellationToken);

    /// <summary>
    /// Graba el abono ya calculado: genera las secuencias, inserta el pago, actualiza el contrato
    /// y la caja de la empresa e ingresa los movimientos de caja.
    /// </summary>
    Task<NumerosAbonoDto> RegistrarAbonoAsync(RegistroAbonoDto registro, CancellationToken cancellationToken);
}

/// <param name="Interes">Tasa mensual en porcentaje (<c>CONTRATOS.interes</c>).</param>
public sealed record ContratoAbonoDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    DateTime? FechaCreacion,
    DateTime? UltimaFechaPago,
    DateTime? FechaVencimiento,
    decimal SaldoCapital,
    decimal Interes);

public sealed record SesionCobroDto(DateTime? FechaSesion, int DiasGracia);

public sealed record RegistroAbonoDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    DateTime FechaSesion,
    string UsuarioResponsable,
    AbonoCalculado Abono);

/// <param name="NumeroPago">Número del pago (<c>PAGOS.numero_pagos</c>, secuencia <c>AI</c>).</param>
/// <param name="NumeroRecibo">Número de recibo (<c>PAGOS.secuencia_recibo</c>, secuencia <c>SR</c>).</param>
public sealed record NumerosAbonoDto(int NumeroPago, int NumeroRecibo);
