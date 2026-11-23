using System.Text.Json.Serialization;
using GoldmoneyBackend.Application.Empenios.Transacciones;

namespace GoldmoneyBackend.Application.Empenios.Vencidos;

/// <summary>Vencidos: contratos activos que superan su plazo. Retirados: contratos que la casa ya retiró (historial CF).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModoVencidos
{
    Vencidos,
    Retirados
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoFechaFiltro
{
    Todos,
    Creacion,
    Vencimiento
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FiltroMeses
{
    Todos,
    MesesPagados,
    MesesDebe
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperadorMeses
{
    Igual,
    Menor,
    Mayor,
    MenorIgual,
    MayorIgual,
    Diferente
}

public sealed record FiltroContratosVencidos(
    string CodigoEmpresa,
    int? CodigoGrupo,
    ModoVencidos Modo,
    TipoFechaFiltro TipoFecha,
    DateTime? Desde,
    DateTime? Hasta,
    FiltroMeses Filtro,
    OperadorMeses Operador,
    int Meses);

/// <summary>Fecha de trabajo (MAX(fecha_apertura) de la caja) y días de gracia de la empresa.</summary>
public sealed record ContextoVencimiento(DateTime? FechaSistema, int DiasGracia);

/// <summary>Fila cruda de CONTRATOS o HISTORIAL_CONTRATOS con su cliente y grupo.</summary>
public sealed record ContratoVencidoDatos(
    string CodigoEmpresa,
    int CodigoGrupo,
    string? AbreviaturaGrupo,
    string? DescripcionGrupo,
    string NumeroContrato,
    string? Serie,
    string? IdCliente,
    string? Nombre,
    string? Apellido,
    DateTime? FechaCreacion,
    DateTime? FechaVencimiento,
    DateTime? UltimaFechaPago,
    decimal? CapitalPrestado,
    decimal? SaldoActual,
    decimal? SaldoCapital,
    int? PlazoPago,
    string? Observacion,
    DateTime? FechaRetiro);

public sealed record ContratoVencidoDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string? AbreviaturaGrupo,
    string NumeroContrato,
    string Serie,
    string? IdCliente,
    string Cliente,
    int MesesPagados,
    int MesesDebe,
    int? PlazoPago,
    decimal CapitalPrestado,
    decimal SaldoCapital,
    decimal SaldoInteres,
    DateTime? FechaCreacion,
    DateTime? FechaVencimiento,
    DateTime? UltimaFechaPago,
    string? Observacion,
    DateTime? FechaRetiro);

public sealed record TotalesVencidosDto(
    int? CodigoGrupo,
    string? AbreviaturaGrupo,
    string? DescripcionGrupo,
    int Cantidad,
    decimal Capital,
    decimal SaldoCapital,
    decimal SaldoInteres);

public sealed record ContratosVencidosResultDto(
    ModoVencidos Modo,
    DateTime? FechaSistema,
    int DiasGracia,
    int Total,
    bool Truncado,
    TotalesVencidosDto Totales,
    IReadOnlyList<TotalesVencidosDto> Grupos,
    IReadOnlyList<ContratoVencidoDto> Contratos,
    int Pagina,
    int TamanoPagina);

public sealed record PrendaVencidaDto(int Numero, string? Descripcion, decimal? Kilates, decimal? Peso, int? Cantidad);

public sealed record PagoContratoDto(
    int NumeroPago,
    DateTime? FechaPago,
    decimal? CantidadPagos,
    decimal? PagoInteres,
    decimal? PagoCapital,
    decimal? SaldoInteres,
    decimal? SaldoCapital,
    DateTime? PagadoHasta);

/// <summary>Datos que el repositorio entrega para el panel de detalle.</summary>
public sealed record DetalleVencidoDatos(
    IReadOnlyList<PrendaVencidaDto> Prendas,
    IReadOnlyList<PagoContratoDto> Pagos,
    string? ComentarioCliente,
    bool MostrarComentarioCliente,
    bool MostrarComentarioContrato);

public sealed record DetalleContratoVencidoDto(
    ContratoVencidoDto Contrato,
    IReadOnlyList<PrendaVencidaDto> Prendas,
    IReadOnlyList<PagoContratoDto> Pagos,
    string? ObservacionContrato,
    string? ComentarioCliente);

public sealed record ProcesoVencidosResultDto(int Procesados, decimal Capital, IReadOnlyList<string> Contratos);

public sealed record RetiroContratosDatos(
    IReadOnlyList<ContratoClave> Contratos,
    string UsuarioResponsable,
    DateTime FechaIngreso,
    DateTime HoraIngreso);

/// <summary>Clave y capital de un contrato del resultado, para seleccionarlos todos sin traer las filas.</summary>
public sealed record ContratoVencidoClaveDto(string CodigoEmpresa, int CodigoGrupo, string NumeroContrato, string Serie, decimal CapitalPrestado);
