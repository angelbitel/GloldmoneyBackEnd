namespace GoldmoneyBackend.Application.Pagos;

/// <summary>
/// Datos de un contrato activo (tabla CONTRATOS) y de su empresa necesarios para calcular el cobro.
/// </summary>
/// <param name="Interes">Tasa mensual en porcentaje (<c>CONTRATOS.interes</c>).</param>
/// <param name="FechaSesion">Fecha de trabajo de la empresa (sesión abierta en INICIO_CIERRE_SESION).</param>
/// <param name="DiasGracia">Días de gracia de la empresa (<c>PARAMETROS_EMPRESA.dias_gracia</c>).</param>
public sealed record ContratoPagoDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string? AbreviaturaGrupo,
    string NumeroContrato,
    string IdCliente,
    DateTime? FechaCreacion,
    decimal CapitalPrestado,
    decimal Interes,
    decimal SaldoCapital,
    DateTime? UltimaFechaPago,
    DateTime? FechaVencimiento,
    int PlazoPago,
    DateTime FechaSesion,
    int DiasGracia);
