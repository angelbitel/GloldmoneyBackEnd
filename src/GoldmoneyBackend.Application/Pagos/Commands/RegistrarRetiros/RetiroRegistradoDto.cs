namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarRetiros;

/// <summary>Resultado del retiro de un contrato: el comprobante si se registró, o el motivo si no.</summary>
public sealed record RetiroRegistradoDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    bool Registrado,
    string? Error,
    ComprobanteRetiroDto? Comprobante);

/// <param name="PagoInteres">Interés cobrado; negativo si el contrato tenía pagos adelantados.</param>
/// <param name="Total">Lo que entra en caja (capital más interés, salvo que la empresa no reste los adelantos).</param>
public sealed record ComprobanteRetiroDto(
    int NumeroRecibo,
    DateTime FechaPago,
    string IdCliente,
    int MesesQueDebe,
    decimal PagoCapital,
    decimal PagoInteres,
    decimal Total,
    DateTime UltimaFechaPago,
    DateTime FechaVencimiento);
