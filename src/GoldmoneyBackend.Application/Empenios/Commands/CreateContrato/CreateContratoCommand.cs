using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

/// <summary>
/// Empeño nuevo. Solo trae lo que decide el cajero; número, fechas, tasa, plazo e interés los
/// calcula el backend con la sesión abierta y el grupo, como el VB6.
/// </summary>
/// <param name="Nombre">Datos del cliente: solo se usan si el cliente aún no existe en CLIENTES.</param>
public sealed record CreateContratoCommand(
    string CodigoEmpresa,
    int CodigoGrupo,
    string IdCliente,
    string? Nombre,
    string? Apellido,
    string? Direccion,
    string? Telefono,
    string? CodigoPais,
    decimal CapitalPrestado,
    IReadOnlyList<CreateContratoDetalleCommand> Detalles,
    string UsuarioResponsable) : IRequest<CreateContratoResultDto>;

public sealed record CreateContratoDetalleCommand(
    int CodigoTipoPrenda,
    string? Descripcion,
    decimal Peso,
    decimal Kilataje,
    int Cantidad);
