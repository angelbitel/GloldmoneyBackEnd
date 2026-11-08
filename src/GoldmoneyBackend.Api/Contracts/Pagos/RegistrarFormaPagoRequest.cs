namespace GoldmoneyBackend.Api.Contracts.Pagos;

/// <param name="Metodos">Cómo pagó el cliente: efectivo, yappy, tarjeta o transferencia, con su monto.</param>
/// <param name="Documentos">Recibos de abono o retiro que sí se registraron en este cobro.</param>
public sealed record RegistrarFormaPagoRequest(
    string IdCliente,
    IReadOnlyList<MetodoPagoRequest> Metodos,
    IReadOnlyList<DocumentoCobroRequest> Documentos);

public sealed record MetodoPagoRequest(string Metodo, decimal Monto);

/// <param name="Tipo">ABONO o RETIRO.</param>
public sealed record DocumentoCobroRequest(
    string Tipo,
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    int NumeroRecibo,
    decimal Total);
