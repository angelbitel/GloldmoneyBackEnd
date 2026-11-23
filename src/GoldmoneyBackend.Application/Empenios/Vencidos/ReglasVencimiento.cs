using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Application.Empenios.Vencidos;

/// <summary>
/// Reglas de vencimiento de SADE. Port exacto de las funciones SQL dbo.Cantidad_de_Meses y
/// dbo.Meses_que_Debe que usan sp_Contratos_Vencidos_de_la_Empresa y sp_Contratos_Retirados_por_la_Empresa.
/// </summary>
public static class ReglasVencimiento
{
    public const int MaximoContratosPorProceso = 500;

    /// <summary>dbo.Cantidad_de_Meses(@dUltima_Fecha_Pago, @dFecha_Actual, @intDias_Holgura).</summary>
    public static int CantidadDeMeses(DateTime? ultimaFechaPago, DateTime? fechaActual, int diasHolgura)
    {
        if (ultimaFechaPago is null || fechaActual is null) return 0;

        var desde = ultimaFechaPago.Value;
        var hasta = fechaActual.Value;
        var dias = hasta.Day - desde.Day;
        var meses = hasta.Month - desde.Month;
        var anios = (hasta.Year - desde.Year) * 12;

        if (dias <= diasHolgura) return meses + anios;

        // Pasados los días de gracia se cobra el mes en curso. (Las tres ramas del SQL
        // —positivo, cero y negativo— se reducen a la misma suma + 1.)
        return meses + anios + 1;
    }

    /// <summary>dbo.Meses_que_Debe(@dFecha_Contrato, @dUltima_Fecha_Pago, @dFecha_Actual, @intDias_Holgura).</summary>
    public static int MesesQueDebe(DateTime? fechaContrato, DateTime? ultimaFechaPago, DateTime? fechaActual, int diasHolgura)
    {
        if (ultimaFechaPago is null || fechaActual is null) return 0;

        var meses = CantidadDeMeses(ultimaFechaPago, fechaActual, diasHolgura);

        if (meses == 0 && fechaContrato == ultimaFechaPago) return 1;

        // En SQL @intMeses solo se calcula en la rama "else"; en la otra queda NULL y la comparación es falsa.
        var mesesContrato = fechaContrato is null ? (int?)null : CantidadDeMeses(fechaContrato, fechaActual, diasHolgura);
        if (mesesContrato == 0)
            meses = meses < 0 ? (Math.Abs(meses) - 1) * -1 : meses - 1;

        return meses;
    }

    /// <summary>meses_pagados = dbo.Cantidad_de_Meses(fecha_creacion, ultima_fecha_pago, 0).</summary>
    public static int MesesPagados(DateTime? fechaCreacion, DateTime? ultimaFechaPago) =>
        CantidadDeMeses(fechaCreacion, ultimaFechaPago, 0);

    /// <summary>Un contrato está vencido cuando los meses que debe superan su plazo de pago.</summary>
    public static bool EstaVencido(int mesesDebe, int? plazoPago) => plazoPago.HasValue && mesesDebe > plazoPago.Value;

    public static bool CumpleOperador(OperadorMeses operador, int valor, int cantidad) => operador switch
    {
        OperadorMeses.Igual => valor == cantidad,
        OperadorMeses.Menor => valor < cantidad,
        OperadorMeses.Mayor => valor > cantidad,
        OperadorMeses.MenorIgual => valor <= cantidad,
        OperadorMeses.MayorIgual => valor >= cantidad,
        OperadorMeses.Diferente => valor != cantidad,
        _ => false
    };

    public static bool CumpleFiltro(FiltroContratosVencidos filtro, int mesesPagados, int mesesDebe) => filtro.Filtro switch
    {
        FiltroMeses.MesesPagados => CumpleOperador(filtro.Operador, mesesPagados, filtro.Meses),
        FiltroMeses.MesesDebe => CumpleOperador(filtro.Operador, mesesDebe, filtro.Meses),
        _ => true
    };

    /// <summary>Validaciones de Localizar_Contratos_Seleccionados. Devuelve el filtro con el rango normalizado.</summary>
    public static FiltroContratosVencidos Validar(FiltroContratosVencidos filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro.CodigoEmpresa))
            throw new DomainValidationException("Estimado usuario, no ha seleccionado una empresa para localizar los contratos vencidos existentes.");

        if (filtro.Filtro != FiltroMeses.Todos && filtro.Meses < 0)
            throw new DomainValidationException("Estimado usuario, la cantidad de meses no puede ser un valor menor que cero (0).");

        var normalizado = filtro with { CodigoEmpresa = filtro.CodigoEmpresa.Trim() };
        if (filtro.TipoFecha == TipoFechaFiltro.Todos)
            return normalizado with { Desde = null, Hasta = null };

        // Como el SP: si falta una de las fechas se usa la otra.
        var desde = filtro.Desde ?? filtro.Hasta;
        var hasta = filtro.Hasta ?? filtro.Desde;
        if (desde is null || hasta is null)
            throw new DomainValidationException("Estimado usuario, el rango de fecha seleccionado para obtener los contratos vencidos no es válido.");

        if (desde.Value.Date > hasta.Value.Date)
            throw new DomainValidationException("Estimado usuario, la fecha inicial no puede ser mayor que la fecha final.");

        return normalizado with { Desde = desde.Value.Date, Hasta = hasta.Value.Date };
    }
}
