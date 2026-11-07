using FluentAssertions;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Domain.Pagos;

namespace GoldmoneyBackend.Domain.Tests.Pagos;

public sealed class CalculoAbonoTests
{
    private const int DiasGracia = 2;
    private static readonly DateTime FechaSesion = new(2020, 1, 8);

    // Debe una cuota: pagado hasta el 08-dic-2019 y la sesión es el 08-ene-2020.
    private static readonly ContratoParaAbono ContratoQueDebeUnMes = new(
        FechaCreacion: new DateTime(2019, 10, 8),
        UltimaFechaPago: new DateTime(2019, 12, 8),
        FechaVencimiento: new DateTime(2020, 6, 8),
        SaldoCapital: 125m,
        TasaMensual: 10m);

    [Fact]
    public void Calcular_Should_Pay_Owed_Interest_And_Advance_Dates_By_Paid_Months()
    {
        var abono = CalculoAbono.Calcular(ContratoQueDebeUnMes, FechaSesion, DiasGracia, cuotas: 1, abonoCapital: 0m);

        abono.MesesQueDebe.Should().Be(1);
        abono.AbonoInteres.Should().Be(12.50m);
        abono.AbonoCapital.Should().Be(0m);
        abono.Total.Should().Be(12.50m);
        abono.NuevoSaldoCapital.Should().Be(125m);
        abono.NuevoSaldoInteres.Should().Be(0m);
        abono.NuevoInteresMensual.Should().Be(12.50m);
        abono.NuevaUltimaFechaPago.Should().Be(new DateTime(2020, 1, 8));
        abono.NuevoVencimiento.Should().Be(new DateTime(2020, 7, 8));
    }

    [Fact]
    public void Calcular_Should_Reduce_Capital_And_Monthly_Interest_When_Paying_Capital()
    {
        var abono = CalculoAbono.Calcular(ContratoQueDebeUnMes, FechaSesion, DiasGracia, cuotas: 1, abonoCapital: 25m);

        abono.Total.Should().Be(37.50m);
        abono.NuevoSaldoCapital.Should().Be(100m);
        abono.NuevoInteresMensual.Should().Be(10m);
        abono.NuevoSaldoInteres.Should().Be(0m);
    }

    [Fact]
    public void Calcular_Should_Leave_Negative_Interest_Balance_When_Paying_In_Advance()
    {
        var abono = CalculoAbono.Calcular(ContratoQueDebeUnMes, FechaSesion, DiasGracia, cuotas: 3, abonoCapital: 0m);

        abono.AbonoInteres.Should().Be(37.50m);
        abono.NuevoSaldoInteres.Should().Be(-25m);
        abono.NuevaUltimaFechaPago.Should().Be(new DateTime(2020, 3, 8));
        abono.NuevoVencimiento.Should().Be(new DateTime(2020, 9, 8));
    }

    [Fact]
    public void Calcular_Should_Allow_Only_Capital_When_Contract_Owes_Nothing()
    {
        var alDia = ContratoQueDebeUnMes with { UltimaFechaPago = new DateTime(2020, 1, 8) };

        var abono = CalculoAbono.Calcular(alDia, FechaSesion, DiasGracia, cuotas: 0, abonoCapital: 25m);

        abono.MesesQueDebe.Should().Be(0);
        abono.AbonoInteres.Should().Be(0m);
        abono.Total.Should().Be(25m);
        abono.NuevaUltimaFechaPago.Should().Be(alDia.UltimaFechaPago);
        abono.NuevoVencimiento.Should().Be(alDia.FechaVencimiento);
    }

    [Fact]
    public void Calcular_Should_Reject_No_Installments_When_Contract_Owes_Months()
    {
        var calcular = () => CalculoAbono.Calcular(ContratoQueDebeUnMes, FechaSesion, DiasGracia, cuotas: 0, abonoCapital: 25m);

        calcular.Should().Throw<DomainValidationException>().WithMessage("*meses adeudados*");
    }

    [Fact]
    public void Calcular_Should_Reject_Empty_Payment_When_Contract_Owes_Nothing()
    {
        var alDia = ContratoQueDebeUnMes with { UltimaFechaPago = new DateTime(2020, 1, 8) };

        var calcular = () => CalculoAbono.Calcular(alDia, FechaSesion, DiasGracia, cuotas: 0, abonoCapital: 0m);

        calcular.Should().Throw<DomainValidationException>().WithMessage("*abono a capital no puede estar en blanco*");
    }

    [Fact]
    public void Calcular_Should_Reject_Capital_When_Installments_Do_Not_Cover_The_Debt()
    {
        var debeDosMeses = ContratoQueDebeUnMes with { UltimaFechaPago = new DateTime(2019, 11, 8) };

        var calcular = () => CalculoAbono.Calcular(debeDosMeses, FechaSesion, DiasGracia, cuotas: 1, abonoCapital: 25m);

        calcular.Should().Throw<DomainValidationException>().WithMessage("*la deuda supera los meses a pagar*");
    }

    [Theory]
    [InlineData(125)]
    [InlineData(130)]
    public void Calcular_Should_Reject_Capital_Equal_Or_Greater_Than_Balance(decimal abonoCapital)
    {
        var calcular = () => CalculoAbono.Calcular(ContratoQueDebeUnMes, FechaSesion, DiasGracia, cuotas: 1, abonoCapital);

        calcular.Should().Throw<DomainValidationException>().WithMessage("*retiro*");
    }
}
