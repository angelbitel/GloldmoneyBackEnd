using FluentAssertions;
using GoldmoneyBackend.Application.Empenios.Vencidos;

namespace GoldmoneyBackend.Application.Tests.Empenios;

public sealed class BusquedaVencidosTests
{
    private static ContratoVencidoDto C(string numero, string cedula, string cliente) => new(
        "1", 1, "OR", numero, numero, cedula, cliente, 1, 8, 6, 100m, 100m, 10m, null, null, null, null, null);

    private static readonly List<ContratoVencidoDto> Lista =
    [
        C("110000000001", "8-123-456", "MARIA PEREZ"),
        C("110000000012", "8-999-111", "JUAN GOMEZ"),
        C("110000000123", "8-123-789", "Maria Lopez"),
    ];

    [Fact]
    public void Numero_exacto_devuelve_solo_ese_contrato()
    {
        BusquedaVencidos.Filtrar(Lista, " 110000000012 ").Should().ContainSingle()
            .Which.Cliente.Should().Be("JUAN GOMEZ");
    }

    [Fact]
    public void Busca_por_nombre_sin_distinguir_mayusculas_y_por_cedula()
    {
        BusquedaVencidos.Filtrar(Lista, "maria").Should().HaveCount(2);
        BusquedaVencidos.Filtrar(Lista, "8-123").Should().HaveCount(2);
        BusquedaVencidos.Filtrar(Lista, "").Should().HaveCount(3);
    }

    [Fact]
    public void Pagina_de_diez_y_normaliza_valores_invalidos()
    {
        var muchos = Enumerable.Range(1, 25).Select(i => C($"{i:000000000000}", "x", "y")).ToList();
        var (pagina, tamano) = BusquedaVencidos.NormalizarPagina(0, 0);
        (pagina, tamano).Should().Be((1, 1));

        BusquedaVencidos.Paginar(muchos, 3, 10).Should().HaveCount(5);
        BusquedaVencidos.Paginar(muchos, 1, 10).First().NumeroContrato.Should().Be("000000000001");
    }
}
