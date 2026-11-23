namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>Consultas de solo lectura para las pantallas de Administración (Inventario y Caja).</summary>
public interface IAdministracionReadService
{
    Task<InventarioDto> GetInventarioAsync(string codigoEmpresa, int? codigoGrupo, string? buscar, CancellationToken cancellationToken);

    /// <summary>null si la empresa no existe.</summary>
    Task<EstadoCajaDto?> GetEstadoCajaAsync(string codigoEmpresa, CancellationToken cancellationToken);
}

public sealed record InventarioItemDto(
    string CodigoEmpresa,
    int CodigoGrupo,
    string? AbreviaturaGrupo,
    string NumeroContrato,
    string? Serie,
    string IdCliente,
    string? Cliente,
    DateTime? FechaCreacion,
    DateTime? FechaVencimiento,
    decimal CapitalPrestado,
    decimal SaldoCapital,
    int Prendas,
    decimal PesoTotal,
    decimal? KilatesMaximo,
    bool Vencido);

public sealed record InventarioDto(
    int TotalContratos,
    decimal TotalCapital,
    decimal TotalSaldoCapital,
    decimal TotalGramos,
    int Vencidos,
    IReadOnlyList<InventarioItemDto> Contratos);

public sealed record MovimientoCajaResumenDto(
    string CodigoTransaccion,
    string Descripcion,
    /// <summary>"Ingreso" o "Egreso" de caja.</summary>
    string Tipo,
    int Cantidad,
    decimal Monto);

public sealed record EstadoCajaDto(
    string CodigoEmpresa,
    string? NombreEmpresa,
    string CodigoEmpresaCaja,
    bool SesionAbierta,
    DateTime? FechaSesion,
    decimal? ValorInicialCaja,
    decimal? MontoInicial,
    decimal MontoActual,
    decimal TotalIngresos,
    decimal TotalEgresos,
    IReadOnlyList<MovimientoCajaResumenDto> Movimientos,
    /// <summary>Otras empresas cuya caja maneja esta (EMPRESA.cod_empresa_caja); se cierran con ella.</summary>
    IReadOnlyList<EmpresaLigadaDto> EmpresasLigadas);

public sealed record EmpresaLigadaDto(string CodigoEmpresa, string? NombreEmpresa);
