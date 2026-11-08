namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>
/// Forma de pago de los cobros. Va en tablas propias del sistema nuevo (Cobros*): el VB6 no
/// registra la forma de pago y no tiene dónde guardarla.
/// </summary>
public interface ICobrosRepository
{
    /// <summary>
    /// Guarda el cobro con sus métodos de pago y los recibos que cubre. Lanza
    /// ConflictDomainException si alguno de esos recibos ya tiene forma de pago registrada.
    /// </summary>
    Task<CobroDto> RegistrarAsync(RegistroCobroDto cobro, CancellationToken cancellationToken);

    Task<CobroDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record RegistroCobroDto(
    string IdCliente,
    string Usuario,
    DateTime FechaUtc,
    IReadOnlyList<MetodoPagoCobroDto> Metodos,
    IReadOnlyList<DocumentoCobroDto> Documentos);

public sealed record MetodoPagoCobroDto(string Metodo, decimal Monto);

/// <param name="Tipo">ABONO o RETIRO.</param>
/// <param name="NumeroRecibo">Número del comprobante que devolvió el registro del abono o retiro.</param>
public sealed record DocumentoCobroDto(
    string Tipo,
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    int NumeroRecibo,
    decimal Total);

/// <param name="TotalMetodos">Lo que el cajero declaró recibir, sumando los métodos.</param>
/// <param name="TotalDocumentos">Lo que efectivamente se registró en los contratos. Si difiere de
/// <paramref name="TotalMetodos"/>, se rechazó parte de un cobro con pago combinado.</param>
public sealed record CobroDto(
    Guid Id,
    DateTime FechaUtc,
    string Usuario,
    string IdCliente,
    decimal TotalMetodos,
    decimal TotalDocumentos,
    IReadOnlyList<MetodoPagoCobroDto> Metodos,
    IReadOnlyList<DocumentoCobroDto> Documentos);
