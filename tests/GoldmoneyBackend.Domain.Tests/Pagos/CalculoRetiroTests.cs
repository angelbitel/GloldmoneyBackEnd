using FluentAssertions;
using GoldmoneyBackend.Domain.Pagos;

namespace GoldmoneyBackend.Domain.Tests.Pagos;

public sealed class CalculoRetiroTests
{
    private const int DiasGracia = 2;

    // Contrato 110000068905 del VB6: creado el 05-dic-2019, sin pagos, retirado el 07-ene-2020.
    private static readonly ContratoParaRetiro ContratoQueDebeUnMes = new(
        FechaCreacion: new DateTime(2019, 12, 5),
        UltimaFechaPago: new DateTime(2019, 12, 5),
        FechaVencimiento: new DateTime(2020, 6, 5),
        SaldoCapital: 125m,
        TasaMensual: 10m);

    [Fact]
    public void Calcular_Should_Charge_Capital_And_Owed_Interest_And_Advance_Dates()
    {
        var retiro = CalculoRetiro.Calcular(ContratoQueDebeUnMes, new DateTime(2020, 1, 7), DiasGracia, restarAbonoCapital: true);

        retiro.MesesQueDebe.Should().Be(1);
        retiro.PagoCapital.Should().Be(125m);
        retiro.PagoInteres.Should().Be(12.50m);
        retiro.Total.Should().Be(137.50m);
        retiro.NuevaUltimaFechaPago.Should().Be(new DateTime(2020, 1, 5));
        retiro.NuevoVencimiento.Should().Be(new DateTime(2020, 7, 5));
    }

    [Fact]
    public void Calcular_Should_Subtract_Advance_Payments_When_Company_Allows_It()
    {
        var adelantado = ContratoQueDebeUnMes with { UltimaFechaPago = new DateTime(2020, 3, 5) };

        var retiro = CalculoRetiro.Calcular(adelantado, new DateTime(2020, 1, 7), DiasGracia, restarAbonoCapital: true);

        retiro.MesesQueDebe.Should().Be(-2);
        retiro.PagoInteres.Should().Be(-25m);
        retiro.Total.Should().Be(100m);
        retiro.NuevaUltimaFechaPago.Should().Be(new DateTime(2020, 1, 5));
    }

    [Fact]
    public void Calcular_Should_Charge_Only_Capital_When_Company_Does_Not_Subtract_Advance_Payments()
    {
        var adelantado = ContratoQueDebeUnMes with { UltimaFechaPago = new DateTime(2020, 3, 5) };

        var retiro = CalculoRetiro.Calcular(adelantado, new DateTime(2020, 1, 7), DiasGracia, restarAbonoCapital: false);

        retiro.PagoInteres.Should().Be(-25m);
        retiro.Total.Should().Be(125m);
    }
}
