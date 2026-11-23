using FluentAssertions;
using GoldmoneyBackend.Application.Caja;
using GoldmoneyBackend.Application.Empenios.Vencidos;
using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Application.Tests.Empenios;

public sealed class ReglasVencimientoTests
{
    /// <summary>
    /// Valores obtenidos ejecutando dbo.Cantidad_de_Meses y dbo.Meses_que_Debe en la BD SADE:
    /// creación, última fecha de pago, fecha actual, días de gracia, Cantidad_de_Meses(pago, actual),
    /// Meses_que_Debe y meses pagados (Cantidad_de_Meses(creación, pago, 0)).
    /// </summary>
    [Theory]
    [InlineData("2026-01-15", "2026-01-15", "2026-01-20", 5, 0, 1, 0)]
    [InlineData("2026-01-15", "2026-01-15", "2026-01-15", 5, 0, 1, 0)]
    [InlineData("2026-01-15", "2026-03-15", "2026-02-10", 5, -1, -1, 2)]
    [InlineData("2025-06-10", "2025-09-10", "2026-11-17", 5, 15, 15, 3)]
    [InlineData("2025-06-10", "2025-06-10", "2026-11-17", 0, 18, 18, 0)]
    [InlineData("2026-10-20", "2026-10-20", "2026-11-17", 3, 1, 1, 0)]
    [InlineData("2026-10-20", "2026-12-20", "2026-11-17", 3, -1, -1, 2)]
    [InlineData("2026-11-01", "2026-11-01", "2026-11-30", 3, 1, 1, 0)]
    [InlineData("2026-11-10", "2027-01-10", "2026-11-17", 3, -1, -1, 2)]
    [InlineData("2026-08-28", "2026-09-28", "2026-11-17", 3, 2, 2, 1)]
    public void Coincide_Con_Las_Funciones_Sql(
        string creacion, string pago, string actual, int gracia, int cantidad, int debe, int pagados)
    {
        var c = DateTime.Parse(creacion);
        var p = DateTime.Parse(pago);
        var a = DateTime.Parse(actual);

        ReglasVencimiento.CantidadDeMeses(p, a, gracia).Should().Be(cantidad);
        ReglasVencimiento.MesesQueDebe(c, p, a, gracia).Should().Be(debe);
        ReglasVencimiento.MesesPagados(c, p).Should().Be(pagados);
    }

    [Fact]
    public void Fechas_Nulas_Devuelven_Cero_Como_IsNull_Del_Sql()
    {
        ReglasVencimiento.CantidadDeMeses(null, DateTime.Today, 3).Should().Be(0);
        ReglasVencimiento.MesesQueDebe(DateTime.Today, DateTime.Today, null, 3).Should().Be(0);
    }

    [Theory]
    [InlineData(5, 4, true)]
    [InlineData(4, 4, false)]
    [InlineData(10, null, false)]
    public void Vencido_Cuando_Debe_Mas_Meses_Que_El_Plazo(int debe, int? plazo, bool vencido) =>
        ReglasVencimiento.EstaVencido(debe, plazo).Should().Be(vencido);

    [Theory]
    [InlineData(OperadorMeses.Igual, 3, 3, true)]
    [InlineData(OperadorMeses.Menor, 2, 3, true)]
    [InlineData(OperadorMeses.Mayor, 3, 3, false)]
    [InlineData(OperadorMeses.MenorIgual, 3, 3, true)]
    [InlineData(OperadorMeses.MayorIgual, 2, 3, false)]
    [InlineData(OperadorMeses.Diferente, 2, 3, true)]
    public void Operadores_Del_Filtro_Adicional(OperadorMeses operador, int valor, int cantidad, bool esperado) =>
        ReglasVencimiento.CumpleOperador(operador, valor, cantidad).Should().Be(esperado);

    [Fact]
    public void Filtro_Por_Meses_Que_Debe_Usa_Meses_Debe()
    {
        var filtro = Filtro() with { Filtro = FiltroMeses.MesesDebe, Operador = OperadorMeses.MayorIgual, Meses = 6 };

        ReglasVencimiento.CumpleFiltro(filtro, mesesPagados: 10, mesesDebe: 6).Should().BeTrue();
        ReglasVencimiento.CumpleFiltro(filtro, mesesPagados: 10, mesesDebe: 5).Should().BeFalse();
    }

    [Fact]
    public void Validar_Completa_La_Fecha_Faltante_Como_El_Sp()
    {
        var desde = new DateTime(2026, 1, 1);
        var filtro = ReglasVencimiento.Validar(Filtro() with { TipoFecha = TipoFechaFiltro.Creacion, Desde = desde, Hasta = null });

        filtro.Hasta.Should().Be(desde);
    }

    [Fact]
    public void Validar_Rechaza_Rango_Invertido_Y_Meses_Negativos()
    {
        var invertido = Filtro() with { TipoFecha = TipoFechaFiltro.Vencimiento, Desde = new DateTime(2026, 2, 1), Hasta = new DateTime(2026, 1, 1) };
        var negativos = Filtro() with { Filtro = FiltroMeses.MesesPagados, Meses = -1 };

        FluentActions.Invoking(() => ReglasVencimiento.Validar(invertido)).Should().Throw<DomainValidationException>();
        FluentActions.Invoking(() => ReglasVencimiento.Validar(negativos)).Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Validar_Ignora_Fechas_Cuando_El_Despliegue_Es_Todos()
    {
        var filtro = ReglasVencimiento.Validar(Filtro() with { Desde = new DateTime(2026, 1, 1) });

        filtro.Desde.Should().BeNull();
        filtro.Hasta.Should().BeNull();
    }

    [Fact]
    public void Movimiento_De_Caja_Valida_Monto_Motivo_Y_Fondos()
    {
        FluentActions.Invoking(() => ReglasMovimientoCaja.Validar(0m, "x")).Should().Throw<DomainValidationException>();
        FluentActions.Invoking(() => ReglasMovimientoCaja.Validar(10.123m, "x")).Should().Throw<DomainValidationException>();
        FluentActions.Invoking(() => ReglasMovimientoCaja.Validar(10m, "  ")).Should().Throw<DomainValidationException>();
        FluentActions.Invoking(() => ReglasMovimientoCaja.Validar(10m, new string('a', 61))).Should().Throw<DomainValidationException>();
        ReglasMovimientoCaja.Validar(10.50m, "  Fondo de caja ").Should().Be((10.50m, "Fondo de caja"));

        FluentActions.Invoking(() => ReglasMovimientoCaja.ValidarFondos(TipoMovimientoManual.Retiro, 101m, 100m))
            .Should().Throw<DomainValidationException>();
        FluentActions.Invoking(() => ReglasMovimientoCaja.ValidarFondos(TipoMovimientoManual.Deposito, 101m, 100m))
            .Should().NotThrow();
    }

    private static FiltroContratosVencidos Filtro() =>
        new("1", null, ModoVencidos.Vencidos, TipoFechaFiltro.Todos, null, null, FiltroMeses.Todos, OperadorMeses.Igual, 0);
}
