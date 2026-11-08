using FluentAssertions;
using GoldmoneyBackend.Domain.Empenios;

namespace GoldmoneyBackend.Domain.Tests.Empenios;

public sealed class CalculoEmpenioTests
{
    [Fact]
    public void Calcular_Should_Match_A_New_VB6_Contract()
    {
        // Contrato 110000069122 del VB6: 1155.00 al 10 % por 6 meses, creado el 08-ene-2020.
        var contrato = CalculoEmpenio.Calcular(1155m, 10m, 6, new DateTime(2020, 1, 8));

        contrato.FechaCreacion.Should().Be(new DateTime(2020, 1, 8));
        contrato.UltimaFechaPago.Should().Be(new DateTime(2020, 1, 8));
        contrato.FechaVencimiento.Should().Be(new DateTime(2020, 7, 8));
        contrato.InteresMensual.Should().Be(115.50m);
    }

    [Fact]
    public void Calcular_Should_Clamp_The_Due_Date_To_The_End_Of_The_Month()
    {
        // Contrato 110000069066 del VB6: creado el 31-dic-2019, vence el 30-jun-2020.
        var contrato = CalculoEmpenio.Calcular(30m, 10m, 6, new DateTime(2019, 12, 31));

        contrato.FechaVencimiento.Should().Be(new DateTime(2020, 6, 30));
        contrato.InteresMensual.Should().Be(3m);
    }

    [Fact]
    public void Calcular_Should_Ignore_The_Time_Of_The_Session_Date()
    {
        var contrato = CalculoEmpenio.Calcular(100m, 10m, 6, new DateTime(2026, 11, 8, 21, 15, 0));

        contrato.FechaCreacion.Should().Be(new DateTime(2026, 11, 8));
        contrato.FechaVencimiento.Should().Be(new DateTime(2027, 5, 8));
    }

    [Theory]
    [InlineData(12.35, 10, 1.24)]   // 1.235 → 1.24, como SQL Server
    [InlineData(33.33, 7.5, 2.50)]  // 2.49975 → 2.50
    public void Calcular_Should_Round_Monthly_Interest_Away_From_Zero(double capital, double tasa, double esperado)
    {
        CalculoEmpenio.Calcular((decimal)capital, (decimal)tasa, 6, new DateTime(2026, 11, 8))
            .InteresMensual.Should().Be((decimal)esperado);
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(100, 0)]
    public void Calcular_Should_Reject_Invalid_Capital_Or_Term(double capital, int plazo)
    {
        var calcular = () => CalculoEmpenio.Calcular((decimal)capital, 10m, plazo, new DateTime(2026, 11, 8));

        calcular.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Numerar_Should_Build_The_VB6_Contract_Number()
    {
        CalculoEmpenio.Numerar("1", 1, 69137).Should().Be(("110000069137", "0000069137"));
    }
}
