namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;

public sealed record ContratoCompletoDto(
    string ContratoId,
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    string? IdCliente,
    DateTime FechaCreacion,
    decimal CapitalPrestado,
    decimal Interes,
    decimal SaldoActual,
    decimal Mensualidad,
    string? Observacion,
    DateTime UltimaFechaPago,
    decimal SaldoCapital,
    DateTime FechaVencimiento,
    int PlazoPago,
    string? UsuarioResponsable,
    decimal MontoMaximo,
    string? Serie,
    IReadOnlyList<ContratoDetalleDto> Detalles);