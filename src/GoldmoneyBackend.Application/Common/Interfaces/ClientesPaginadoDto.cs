namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>Filtro del listado de clientes.</summary>
public enum FiltroClientes
{
    Todos,
    Activos,
    Inactivos,
    ConContratos
}

/// <summary>Cliente del listado con sus contratos activos (saldo_capital &gt; 0).</summary>
public sealed record ClienteListadoDto(
    string IdCliente,
    string? Apellido,
    string? Nombre,
    string? Telefono,
    int? Estatus,
    string? Direccion,
    int ContratosActivos,
    decimal CapitalActivo);

/// <summary>Totales de la tabla CLIENTES (no dependen de la búsqueda).</summary>
public sealed record ResumenClientesDto(int TotalClientes, int Activos, int Inactivos, int ConContratosActivos);

public sealed record ClientesPaginadoDto(
    int Total,
    int Pagina,
    int TamanoPagina,
    ResumenClientesDto Resumen,
    IReadOnlyList<ClienteListadoDto> Clientes);

public static class PaginacionClientes
{
    public const int TamanoPorDefecto = 10;
    public const int TamanoMaximo = 100;

    public static (int Pagina, int TamanoPagina) Normalizar(int pagina, int tamanoPagina) =>
        (Math.Max(pagina, 1), Math.Clamp(tamanoPagina, 1, TamanoMaximo));
}
