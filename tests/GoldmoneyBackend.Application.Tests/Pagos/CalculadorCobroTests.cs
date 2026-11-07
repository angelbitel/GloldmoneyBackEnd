using FluentAssertions;
using GoldmoneyBackend.Application.Pagos;

namespace GoldmoneyBackend.Application.Tests.Pagos;

public sealed class CalculadorCobroTests
{
    [Fact]
    public void ConstruirFila_Should_Compute_Balances_At_Session_Date()
    {
        // Creado el 18-abr, sin pagos, sesión del 26-dic: debe 9 cuotas de 16.00.
        var contrato = ContratoCon(
            fechaCreacion: new DateTime(2019, 4, 18),
            ultimaFechaPago: new DateTime(2019, 4, 18),
            fechaSesion: new DateTime(2019, 12, 26),
            saldoCapital: 160m,
            interes: 10m);

        var fila = CalculadorCobro.ConstruirFila(contrato);

        fila.MesesQueDebe.Should().Be(9);
        fila.InteresMensual.Should().Be(16m);
        fila.SaldoInteres.Should().Be(144m);
        fila.SaldoTotal.Should().Be(304m);
        fila.FechaSesion.Should().Be(new DateTime(2019, 12, 26));
    }

    [Fact]
    public void ConstruirFila_Should_Report_Negative_Interest_When_Payments_Are_Ahead()
    {
        var contrato = ContratoCon(
            fechaCreacion: new DateTime(2019, 1, 10),
            ultimaFechaPago: new DateTime(2019, 8, 10),
            fechaSesion: new DateTime(2019, 5, 20),
            saldoCapital: 150m,
            interes: 10m);

        var fila = CalculadorCobro.ConstruirFila(contrato);

        fila.MesesQueDebe.Should().Be(-2);
        fila.SaldoInteres.Should().Be(-30m);
        fila.SaldoTotal.Should().Be(120m);
    }

    [Fact]
    public void ConstruirFila_Should_Trim_Legacy_Char_Columns()
    {
        var contrato = ContratoCon(
            codigoEmpresa: "1 ",
            codigoGrupo: 5,
            numeroContrato: "000123   ",
            abreviaturaGrupo: "oro ");

        var fila = CalculadorCobro.ConstruirFila(contrato);

        fila.ContratoId.Should().Be("15000123");
        fila.CodigoEmpresa.Should().Be("1");
        fila.NumeroContrato.Should().Be("000123");
        fila.AbreviaturaGrupo.Should().Be("ORO");
    }

    [Fact]
    public void CalcularPagosPendientes_Should_List_Overdue_Installments_Anchored_To_Creation_Day()
    {
        var contrato = ContratoCon(
            fechaCreacion: new DateTime(2019, 1, 10),
            ultimaFechaPago: new DateTime(2019, 3, 10),
            fechaSesion: new DateTime(2019, 5, 13),
            saldoCapital: 200m,
            interes: 10m);

        var resultado = CalculadorCobro.CalcularPagosPendientes(contrato);

        resultado.MesesQueDebe.Should().Be(3);
        resultado.CantidadPagos.Should().Be(3);
        resultado.TotalPagar.Should().Be(60m);
        resultado.InteresMensual.Should().Be(20m);
        resultado.Pagos.Select(p => p.FechaPago).Should().Equal(
            new DateTime(2019, 4, 10),
            new DateTime(2019, 5, 10),
            new DateTime(2019, 6, 10));
        resultado.Pagos.Should().OnlyContain(p => p.PagoInteres == 20m && p.PagoCapital == 0m && p.SaldoCapital == 200m);
        resultado.Pagos.Select(p => p.SaldoInteres).Should().Equal(40m, 20m, 0m);
    }

    [Fact]
    public void CalcularPagosPendientes_Should_Keep_Creation_Day_At_Month_End()
    {
        // Creado el 31-ene: la cuota de febrero cae el 28 y la de marzo vuelve al 31.
        var contrato = ContratoCon(
            fechaCreacion: new DateTime(2019, 1, 31),
            ultimaFechaPago: new DateTime(2019, 1, 31),
            fechaSesion: new DateTime(2019, 4, 5),
            saldoCapital: 100m,
            interes: 10m);

        var resultado = CalculadorCobro.CalcularPagosPendientes(contrato);

        resultado.Pagos.Select(p => p.FechaPago).Should().StartWith(new[]
        {
            new DateTime(2019, 2, 28),
            new DateTime(2019, 3, 31),
        });
    }

    [Fact]
    public void CalcularPagosPendientes_Should_Return_No_Installments_When_Payments_Are_Ahead()
    {
        var contrato = ContratoCon(
            fechaCreacion: new DateTime(2019, 1, 10),
            ultimaFechaPago: new DateTime(2019, 8, 10),
            fechaSesion: new DateTime(2019, 5, 20));

        var resultado = CalculadorCobro.CalcularPagosPendientes(contrato);

        resultado.MesesQueDebe.Should().Be(-2);
        resultado.CantidadPagos.Should().Be(0);
        resultado.Pagos.Should().BeEmpty();
        resultado.TotalPagar.Should().Be(0m);
    }

    [Fact]
    public void CalcularPagosPendientes_Should_Use_Creation_Date_When_Last_Payment_Is_Missing()
    {
        var contrato = ContratoCon(
            fechaCreacion: new DateTime(2019, 4, 18),
            ultimaFechaPago: null,
            fechaSesion: new DateTime(2019, 12, 26),
            saldoCapital: 160m,
            interes: 10m);

        var resultado = CalculadorCobro.CalcularPagosPendientes(contrato);

        resultado.CantidadPagos.Should().Be(9);
        resultado.TotalPagar.Should().Be(144m);
    }

    private static ContratoPagoDto ContratoCon(
        DateTime? fechaCreacion = null,
        DateTime? ultimaFechaPago = null,
        DateTime? fechaSesion = null,
        decimal saldoCapital = 100m,
        decimal interes = 10m,
        int diasGracia = 2,
        string codigoEmpresa = "1",
        int codigoGrupo = 1,
        string numeroContrato = "000001",
        string? abreviaturaGrupo = "ORO")
    {
        var creacion = fechaCreacion ?? new DateTime(2019, 1, 10);

        return new ContratoPagoDto(
            codigoEmpresa,
            codigoGrupo,
            abreviaturaGrupo,
            numeroContrato,
            "8-000-0000",
            creacion,
            saldoCapital,
            interes,
            saldoCapital,
            ultimaFechaPago ?? (fechaCreacion is null ? creacion : null),
            creacion.AddMonths(6),
            6,
            fechaSesion ?? new DateTime(2019, 5, 20),
            diasGracia);
    }
}
