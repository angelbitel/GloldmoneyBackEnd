using FluentAssertions;
using GoldmoneyBackend.Domain.Pagos;

namespace GoldmoneyBackend.Domain.Tests.Pagos;

public sealed class ReglasCobroLegacyTests
{
    // Valores esperados obtenidos de dbo.Meses_que_Debe en la base legacy.
    [Theory]
    [InlineData("2019-04-18", "2019-04-18", "2019-04-19", 2, 1)]  // primer mes, sin pagos: debe la primera cuota
    [InlineData("2019-04-18", "2019-04-18", "2019-04-25", 2, 1)]
    [InlineData("2019-04-18", "2019-04-18", "2019-05-10", 2, 1)]
    [InlineData("2019-04-18", "2019-04-18", "2019-12-26", 2, 9)]
    [InlineData("2019-01-10", "2019-03-10", "2019-05-12", 2, 2)]  // dentro de los días de gracia
    [InlineData("2019-01-10", "2019-03-10", "2019-05-13", 2, 3)]  // pasados los días de gracia
    [InlineData("2019-01-10", "2019-08-10", "2019-05-20", 2, -2)] // pagos adelantados
    [InlineData("2019-05-10", "2019-07-10", "2019-05-11", 2, -1)] // adelantado dentro del primer mes
    public void MesesQueDebe_Should_Match_Legacy_Function(
        string fechaCreacion, string ultimaFechaPago, string fechaSesion, int diasGracia, int esperado)
    {
        var meses = ReglasCobroLegacy.MesesQueDebe(
            DateTime.Parse(fechaCreacion),
            DateTime.Parse(ultimaFechaPago),
            DateTime.Parse(fechaSesion),
            diasGracia);

        meses.Should().Be(esperado);
    }

    [Fact]
    public void MesesQueDebe_Should_Treat_End_Of_February_As_Day_30_Like_VB6()
    {
        // La función SQL devuelve 2; el VB6, que es el que calcula lo que se cobra, devuelve 1.
        var meses = ReglasCobroLegacy.MesesQueDebe(
            new DateTime(2019, 2, 28),
            new DateTime(2019, 2, 28),
            new DateTime(2019, 3, 29),
            0);

        meses.Should().Be(1);
    }

    [Fact]
    public void MesesQueDebe_Should_Ignore_Time_Of_Day_When_Comparing_Creation_And_Last_Payment()
    {
        var meses = ReglasCobroLegacy.MesesQueDebe(
            new DateTime(2019, 4, 18, 15, 34, 8),
            new DateTime(2019, 4, 18),
            new DateTime(2019, 4, 19),
            2);

        meses.Should().Be(1);
    }

    [Fact]
    public void InteresMensual_Should_Apply_Monthly_Rate_To_Capital_Balance()
    {
        ReglasCobroLegacy.InteresMensual(216m, 10m).Should().Be(21.6m);
    }

    [Fact]
    public void SaldoInteres_Should_Be_Negative_When_Payments_Are_Ahead()
    {
        ReglasCobroLegacy.SaldoInteres(150m, 10m, -2).Should().Be(-30m);
    }

    [Fact]
    public void AbonoInteres_Should_Round_To_Cents_Away_From_Zero()
    {
        // 33.33 × 10 % = 3.333 por cuota; 3 cuotas = 9.999 -> 10.00
        ReglasCobroLegacy.AbonoInteres(33.33m, 10m, 3).Should().Be(10.00m);
        // 0.25 × 10 % = 0.025 -> 0.03 (no redondeo bancario)
        ReglasCobroLegacy.AbonoInteres(0.25m, 10m, 1).Should().Be(0.03m);
    }
}
