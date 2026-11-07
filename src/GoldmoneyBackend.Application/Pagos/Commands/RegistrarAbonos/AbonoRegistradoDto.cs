namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarAbonos;

/// <summary>Resultado del abono de un contrato: el comprobante si se registró, o el motivo si no.</summary>
public sealed record AbonoRegistradoDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    bool Registrado,
    string? Error,
    ComprobanteAbonoDto? Comprobante);

/// <param name="NuevaUltimaFechaPago">Fecha hasta la que queda pagado el contrato ("pagado hasta").</param>
public sealed record ComprobanteAbonoDto(
    int NumeroPago,
    int NumeroRecibo,
    DateTime FechaPago,
    int Cuotas,
    decimal AbonoInteres,
    decimal AbonoCapital,
    decimal Total,
    decimal NuevoSaldoCapital,
    decimal NuevoSaldoInteres,
    decimal NuevoInteresMensual,
    DateTime NuevaUltimaFechaPago,
    DateTime NuevoVencimiento);
