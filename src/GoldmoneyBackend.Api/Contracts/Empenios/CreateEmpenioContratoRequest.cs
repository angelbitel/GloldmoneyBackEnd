namespace GoldmoneyBackend.Api.Contracts.Empenios;

/// <summary>
/// Empeño nuevo. Número, fechas, tasa, plazo e interés los calcula el backend con la sesión
/// abierta y el grupo; el usuario responsable sale del token.
/// </summary>
/// <param name="Nombre">Datos del cliente: solo se usan si aún no existe en CLIENTES.</param>
public sealed record CreateEmpenioContratoRequest(
    string CodigoEmpresa,
    int CodigoGrupo,
    string IdCliente,
    string? Nombre,
    string? Apellido,
    string? Direccion,
    string? Telefono,
    string? CodigoPais,
    decimal CapitalPrestado,
    IReadOnlyList<CreateEmpenioDetalleRequest>? Detalles);

public sealed record CreateEmpenioDetalleRequest(
    int CodigoTipoPrenda,
    string? Descripcion,
    decimal Peso,
    decimal Kilataje,
    int Cantidad);
