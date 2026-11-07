using FluentAssertions;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Pagos.Commands.RegistrarAbonos;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldmoneyBackend.Application.Tests.Pagos;

public sealed class RegistrarAbonosCommandTests
{
    private static readonly DateTime FechaSesion = new(2020, 1, 8);

    [Fact]
    public async Task Handle_Should_Register_Payment_With_Legacy_Values_And_Return_Receipt()
    {
        var repositorio = new AbonosFalsos();

        var resultados = await CrearHandler(repositorio).Handle(Comando(Item("C1", cuotas: 1, capital: 25m)), default);

        var resultado = resultados.Should().ContainSingle().Subject;
        resultado.Registrado.Should().BeTrue();
        resultado.Comprobante!.NumeroRecibo.Should().Be(500);
        resultado.Comprobante.Total.Should().Be(37.50m);

        var registro = repositorio.Registrados.Should().ContainSingle().Subject;
        registro.FechaSesion.Should().Be(FechaSesion);
        registro.UsuarioResponsable.Should().Be("cajero1");
        registro.Abono.NuevoSaldoCapital.Should().Be(100m);
        repositorio.Confirmadas.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_Keep_Other_Contracts_When_One_Fails()
    {
        var repositorio = new AbonosFalsos { FallarEn = "C2" };

        var resultados = await CrearHandler(repositorio).Handle(
            Comando(Item("C1"), Item("C2"), Item("NOEXISTE"), Item("C3")),
            default);

        resultados.Select(r => r.Registrado).Should().Equal(true, false, false, true);
        resultados[1].Error.Should().Be("Estimado usuario, ocurrió un error al momento de abonar el contrato.");
        resultados[2].Error.Should().Contain("no existe o ya fue retirado");
        repositorio.Registrados.Select(r => r.NumeroContrato).Should().Equal("C1", "C3");
        repositorio.Confirmadas.Should().Be(2);
        repositorio.Revertidas.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_Reject_Payment_When_Company_Has_No_Open_Session()
    {
        var repositorio = new AbonosFalsos { Sesion = new SesionCobroDto(null, 2) };

        var resultados = await CrearHandler(repositorio).Handle(Comando(Item("C1")), default);

        resultados[0].Registrado.Should().BeFalse();
        resultados[0].Error.Should().Contain("sesión abierta");
        repositorio.Registrados.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Return_Domain_Rule_Message_When_Payment_Is_Invalid()
    {
        var resultados = await CrearHandler(new AbonosFalsos()).Handle(Comando(Item("C1", cuotas: 0, capital: 25m)), default);

        resultados[0].Registrado.Should().BeFalse();
        resultados[0].Error.Should().Contain("meses adeudados");
    }

    [Fact]
    public void Validator_Should_Reject_Duplicated_Contracts_And_Empty_Lines()
    {
        var validator = new RegistrarAbonosCommandValidator();

        validator.Validate(Comando(Item("C1"), Item("C1"))).IsValid.Should().BeFalse();
        validator.Validate(Comando(Item("C1", cuotas: 0, capital: 0m))).IsValid.Should().BeFalse();
        validator.Validate(Comando(Item("C1", capital: 10.555m))).IsValid.Should().BeFalse();
        validator.Validate(Comando(Item("C1"), Item("C2", cuotas: 0, capital: 5m))).IsValid.Should().BeTrue();
    }

    private static RegistrarAbonosCommandHandler CrearHandler(AbonosFalsos repositorio) =>
        new(repositorio, NullLogger<RegistrarAbonosCommandHandler>.Instance);

    private static RegistrarAbonosCommand Comando(params AbonoContratoItem[] items) => new(items, "cajero1");

    private static AbonoContratoItem Item(string numeroContrato, int cuotas = 1, decimal capital = 0m) =>
        new("1", 1, numeroContrato, cuotas, capital);

    private sealed class AbonosFalsos : IAbonosRepository
    {
        private int _numeroPago = 100;
        private int _numeroRecibo = 499;

        public SesionCobroDto Sesion { get; init; } = new(FechaSesion, 2);
        public string? FallarEn { get; init; }
        public List<RegistroAbonoDto> Registrados { get; } = [];
        public int Confirmadas { get; private set; }
        public int Revertidas { get; private set; }

        public async Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken)
        {
            try
            {
                var resultado = await operacion(cancellationToken);
                Confirmadas++;
                return resultado;
            }
            catch
            {
                Revertidas++;
                throw;
            }
        }

        public Task<ContratoAbonoDto?> GetContratoBloqueadoAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken) =>
            Task.FromResult(numeroContrato == "NOEXISTE"
                ? null
                : new ContratoAbonoDto(codigoEmpresa, codigoGrupo, numeroContrato,
                    new DateTime(2019, 10, 8), new DateTime(2019, 12, 8), new DateTime(2020, 6, 8), 125m, 10m));

        public Task<SesionCobroDto> GetSesionAbiertaAsync(string codigoEmpresa, CancellationToken cancellationToken) =>
            Task.FromResult(Sesion);

        public Task<NumerosAbonoDto> RegistrarAbonoAsync(RegistroAbonoDto registro, CancellationToken cancellationToken)
        {
            if (registro.NumeroContrato == FallarEn)
            {
                throw new InvalidOperationException("Fallo simulado de base de datos.");
            }

            Registrados.Add(registro);
            return Task.FromResult(new NumerosAbonoDto(++_numeroPago, ++_numeroRecibo));
        }
    }
}
