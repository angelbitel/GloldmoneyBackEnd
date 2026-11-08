namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>
/// Contrato listo para grabar en CONTRATOS. Todos los importes y fechas los calcula el backend
/// (sesión abierta, grupo y secuencia), como el VB6.
/// </summary>
public sealed record CrearEmpenioContratoDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    string Serie,
    string IdCliente,
    DateTime FechaCreacion,
    decimal CapitalPrestado,
    decimal TasaInteres,
    decimal InteresMensual,
    DateTime UltimaFechaPago,
    DateTime FechaVencimiento,
    int PlazoMeses,
    string UsuarioResponsable);

public sealed record CrearEmpenioDetalleDto(
    int CodigoTipoPrenda,
    string? Descripcion,
    decimal Peso,
    decimal Kilataje,
    int Cantidad);

/// <summary>Grupo (GRUPOS) con lo necesario para crear un contrato.</summary>
public sealed record GrupoEmpenioDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string? Abreviatura,
    decimal TasaInteres,
    int PlazoMeses,
    bool Activo);
