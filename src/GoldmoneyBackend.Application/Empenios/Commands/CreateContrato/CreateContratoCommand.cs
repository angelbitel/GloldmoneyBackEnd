using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

public sealed record CreateContratoCommand(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    string? IdCliente,
    string Serie,
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
    string Nombre,
    string Apellido,
    string? Direccion,
    string? Telefono,
    decimal MontoMaximo,
    string UsuarioResponsable,
    string CodigoPais,
    string ProcesoKey,
    decimal? MontoMinimoEmpenio,
    bool? ControlaCajaPorUsuario,
    bool? ConfirmadoPorUsuario,
    int? TrabajoConKilates,
    int? ControlReloj,
    string? TipoTransaccion,
    IReadOnlyList<CreateContratoDetalleCommand>? Detalles) : IRequest<CreateContratoResultDto>;

public sealed record CreateContratoDetalleCommand(
    string CodigoTipoPrenda,
    string? Descripcion,
    decimal? Peso,
    decimal? Kilataje,
    int? Cantidad,
    decimal? MontoAvaluo,
    decimal? MontoPrestamoDetalle,
    string? Observacion);
