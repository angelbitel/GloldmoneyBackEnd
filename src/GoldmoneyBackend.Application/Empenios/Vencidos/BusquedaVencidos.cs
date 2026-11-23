namespace GoldmoneyBackend.Application.Empenios.Vencidos;

/// <summary>Búsqueda libre y paginación del listado de contratos vencidos/retirados.</summary>
public static class BusquedaVencidos
{
    public const int TamanoPaginaPorDefecto = 10;
    public const int TamanoPaginaMaximo = 1000;

    /// <summary>
    /// Filtra por número de contrato/serie, cédula, nombre o apellido (sin distinguir mayúsculas).
    /// Si el término coincide exactamente con un número de contrato, devuelve solo ese.
    /// </summary>
    public static List<ContratoVencidoDto> Filtrar(List<ContratoVencidoDto> contratos, string? buscar)
    {
        var termino = buscar?.Trim();
        if (string.IsNullOrEmpty(termino)) return contratos;

        var exactos = contratos
            .Where(c => Igual(c.NumeroContrato, termino) || Igual(c.Serie, termino))
            .ToList();
        if (exactos.Count > 0) return exactos;

        return contratos
            .Where(c => Contiene(c.NumeroContrato, termino)
                || Contiene(c.Serie, termino)
                || Contiene(c.IdCliente, termino)
                || Contiene(c.Cliente, termino))
            .ToList();
    }

    public static (int Pagina, int TamanoPagina) NormalizarPagina(int pagina, int tamanoPagina) =>
        (Math.Max(pagina, 1), Math.Clamp(tamanoPagina, 1, TamanoPaginaMaximo));

    public static List<ContratoVencidoDto> Paginar(IReadOnlyList<ContratoVencidoDto> contratos, int pagina, int tamanoPagina) =>
        contratos.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

    private static bool Igual(string? valor, string termino) =>
        string.Equals(valor?.Trim(), termino, StringComparison.OrdinalIgnoreCase);

    private static bool Contiene(string? valor, string termino) =>
        valor is not null && valor.Contains(termino, StringComparison.OrdinalIgnoreCase);
}
