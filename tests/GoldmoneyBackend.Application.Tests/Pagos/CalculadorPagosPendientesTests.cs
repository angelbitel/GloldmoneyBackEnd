using FluentAssertions;
using GoldmoneyBackend.Application.Pagos.Queries.GetPagosPendientes;

namespace GoldmoneyBackend.Application.Tests.Pagos;

public sealed class CalculadorPagosPendientesTests
{
    [Fact]
    public void Calcular_Should_Count_Overdue_Payments_Up_To_Vencimiento()
    {
        var hoy = DateTime.Today;
        var contrato = ContratoCon(
            ultimaFechaPago: hoy.AddMonths(-6),
            fechaVencimiento: hoy.AddMonths(-1));

        var resultado = CalculadorPagosPendientes.Calcular(contrato);

        // 5 cuotas entre la última fecha de pago y el vencimiento, aunque el
        // vencimiento ya haya pasado (cuotas vencidas pendientes).
        resultado.CantidadPagos.Should().Be(5);
        resultado.Pagos.Should().HaveCount(5);
        resultado.TotalPagar.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Calcular_Should_Return_No_Payments_When_Last_Payment_Reached_Vencimiento()
    {
        var hoy = DateTime.Today;
        var contrato = ContratoCon(
            ultimaFechaPago: hoy.AddMonths(-1),
            fechaVencimiento: hoy.AddMonths(-1));

        var resultado = CalculadorPagosPendientes.Calcular(contrato);

        resultado.CantidadPagos.Should().Be(0);
        resultado.TotalPagar.Should().Be(0m);
        resultado.Pagos.Should().BeEmpty();
    }

    [Fact]
    public void Calcular_Should_Return_No_Payments_When_Vencimiento_Is_Missing()
    {
        var contrato = ContratoCon(
            ultimaFechaPago: DateTime.Today.AddMonths(-1),
            fechaVencimiento: null);

        var resultado = CalculadorPagosPendientes.Calcular(contrato);

        resultado.CantidadPagos.Should().Be(0);
        resultado.TotalPagar.Should().Be(0m);
    }

    [Fact]
    public void Calcular_Should_Generate_All_Pending_Payments_With_Interest_And_Capital_Split()
    {
        var hoy = DateTime.Today;
        var contrato = ContratoCon(
            ultimaFechaPago: hoy.AddMonths(-3),
            fechaVencimiento: hoy.AddMonths(3),
            saldoCapital: 1000m,
            interes: 10m,
            interesMensual: 150m);

        var resultado = CalculadorPagosPendientes.Calcular(contrato);

        resultado.CantidadPagos.Should().Be(6);
        resultado.Pagos.Should().HaveCount(6);
        resultado.TotalPagar.Should().Be(900m);

        var primerPago = resultado.Pagos[0];
        primerPago.NumeroPago.Should().Be(1);
        primerPago.PagoInteres.Should().Be(100m);
        primerPago.PagoCapital.Should().Be(50m);
        primerPago.SaldoCapital.Should().Be(950m);
        primerPago.SaldoInteres.Should().Be(0m);
        primerPago.Subtotal.Should().Be(150m);

        var segundoPago = resultado.Pagos[1];
        segundoPago.PagoInteres.Should().Be(95m);
        segundoPago.PagoCapital.Should().Be(55m);
        segundoPago.SaldoCapital.Should().Be(895m);
    }

    [Fact]
    public void Calcular_Should_Generate_Interest_Only_Payments_When_No_Mensualidad()
    {
        var hoy = DateTime.Today;
        var contrato = ContratoCon(
            ultimaFechaPago: hoy.AddMonths(-1),
            fechaVencimiento: hoy.AddMonths(1),
            saldoCapital: 1000m,
            interes: 10m,
            interesMensual: 0m);

        var resultado = CalculadorPagosPendientes.Calcular(contrato);

        resultado.CantidadPagos.Should().Be(2);
        resultado.Pagos.Should().OnlyContain(p => p.PagoCapital == 0m);
        resultado.Pagos.Should().OnlyContain(p => p.PagoInteres == 100m);
        resultado.Pagos.Should().OnlyContain(p => p.SaldoCapital == 1000m);
        resultado.TotalPagar.Should().Be(200m);
    }

    [Fact]
    public void Calcular_Should_Use_Fecha_Creacion_As_Reference_When_No_Last_Payment()
    {
        var hoy = DateTime.Today;
        var contrato = ContratoCon(
            fechaCreacion: hoy.AddMonths(-2),
            ultimaFechaPago: null,
            fechaVencimiento: hoy.AddMonths(2),
            saldoCapital: 1000m,
            interes: 10m,
            interesMensual: 0m);

        var resultado = CalculadorPagosPendientes.Calcular(contrato);

        resultado.CantidadPagos.Should().Be(4);
        resultado.TotalPagar.Should().Be(400m);
    }

    [Fact]
    public void Calcular_Should_Build_Composite_ContratoId()
    {
        var hoy = DateTime.Today;
        var contrato = ContratoCon(
            ultimaFechaPago: hoy.AddMonths(-1),
            fechaVencimiento: hoy.AddMonths(1),
            codigoEmpresa: "01",
            codigoGrupo: 5,
            numeroContrato: "000123");

        var resultado = CalculadorPagosPendientes.Calcular(contrato);

        resultado.ContratoId.Should().Be("015000123");
    }

    private static ContratoPagoDto ContratoCon(
        DateTime? ultimaFechaPago = null,
        DateTime? fechaVencimiento = null,
        DateTime? fechaCreacion = null,
        decimal saldoCapital = 1000m,
        decimal interes = 3.5m,
        decimal interesMensual = 100m,
        string codigoEmpresa = "01",
        int codigoGrupo = 1,
        string numeroContrato = "000001")
    {
        return new ContratoPagoDto(
            codigoEmpresa,
            codigoGrupo,
            numeroContrato,
            fechaCreacion,
            interes,
            interesMensual,
            ultimaFechaPago,
            saldoCapital,
            fechaVencimiento);
    }
}