namespace GoldmoneyBackend.Application.Empenios.Transacciones;

/// <summary>Clave compuesta de un contrato legacy (CONTRATOS).</summary>
public sealed record ContratoClave(string CodigoEmpresa, int CodigoGrupo, string NumeroContrato)
{
    public ContratoClave Normalizada() => new(CodigoEmpresa.Trim(), CodigoGrupo, NumeroContrato.Trim());
}

public sealed record DetalleTransaccionDto(
    int Secuencia,
    string? Descripcion,
    decimal? Kilates,
    decimal? Peso,
    int? CantidadProducto,
    string? Categoria);

/// <summary>Datos crudos de un contrato tal como los devuelve el repositorio.</summary>
public sealed record ContratoTransaccionDatos(
    string CodigoEmpresa,
    string? NombreEmpresa,
    int CodigoGrupo,
    string? AbreviaturaGrupo,
    string NumeroContrato,
    string? Serie,
    string IdCliente,
    decimal CapitalPrestado,
    DateTime? FechaCreacion,
    decimal? Interes,
    int? PlazoPago,
    DateTime? FechaVencimiento,
    decimal? InteresMensual,
    decimal? SaldoCapital,
    string? Observacion,
    DateTime? HoraTransaccion,
    string? UsuarioResponsable,
    int CantidadCopias,
    bool TienePagos,
    IReadOnlyList<DetalleTransaccionDto> Detalles);

/// <summary>Parámetros de PARAMETROS_EMPRESA usados por los procesos transaccionales.</summary>
public sealed record ParametrosTransaccionDto(
    decimal? CostoCopia,
    bool ControlarAnulacion,
    bool AnulacionControladaTiempo,
    int? TiempoAnulacionMinutos);

/// <summary>Sesión abierta de la empresa que maneja la caja del contrato.</summary>
public sealed record SesionOperacionDto(string CodigoEmpresaCaja, DateTime FechaApertura);

/// <summary>Contrato listo para la pantalla de administración de empeño.</summary>
public sealed record ContratoTransaccionDto(
    string CodigoEmpresa,
    string? NombreEmpresa,
    int CodigoGrupo,
    string? AbreviaturaGrupo,
    string NumeroContrato,
    string? Serie,
    string IdCliente,
    decimal CapitalPrestado,
    DateTime? FechaCreacion,
    decimal? Interes,
    int? PlazoPago,
    DateTime? FechaVencimiento,
    decimal? InteresMensual,
    decimal? SaldoCapital,
    string? Observacion,
    DateTime? HoraTransaccion,
    string? UsuarioResponsable,
    string Estado,
    int CantidadCopias,
    bool TienePagos,
    DateTime? FechaSesion,
    decimal? CostoCopia,
    bool PuedeAnular,
    string? MotivoNoAnulable,
    bool RequiereSupervisor,
    IReadOnlyList<DetalleTransaccionDto> Detalles);

/// <summary>Estado administrativo del contrato (para /completo y la pantalla de administración).</summary>
public sealed record AdministracionContratoDto(
    string? NombreEmpresa,
    string? AbreviaturaGrupo,
    string Estado,
    int CantidadCopias,
    bool TienePagos,
    DateTime? FechaSesion,
    DateTime? HoraTransaccion,
    decimal? CostoCopia,
    bool PuedeAnular,
    string? MotivoNoAnulable,
    bool RequiereSupervisor);

public sealed record RegistrarCopiaReciboDatos(
    ContratoClave Contrato,
    string IdCliente,
    string CodigoEmpresaCaja,
    DateTime FechaOperacion,
    DateTime Hora,
    decimal Monto,
    string UsuarioResponsable);

public sealed record CopiaReciboRegistradaDto(int NumeroCopia, int SecuenciaRecibo);

public sealed record CopiaReciboResultDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    string? Serie,
    string IdCliente,
    int NumeroCopia,
    int SecuenciaRecibo,
    decimal Monto,
    DateTime Fecha,
    string UsuarioResponsable);

public sealed record AnularContratoDatos(
    ContratoClave Contrato,
    string CodigoEmpresaCaja,
    DateTime FechaOperacion,
    DateTime Hora,
    string UsuarioResponsable);

public sealed record AnularContratoResultDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    decimal MontoDevueltoACaja);
