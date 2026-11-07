using FluentAssertions;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Pagos.Commands.RegistrarRetiros;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldmoneyBackend.Application.Tests.Pagos;

public sealed class RegistrarRetirosCommandTests
{
    private static readonly DateTime FechaSesion = new(2020, 1, 7);

    [Fact]
    public async Task Handle_Should_Register_Withdrawal_And_Return_Receipt()
    {
        var repositorio = new RetirosFalsos();

        var resultados = await CrearHandler(repositorio).Handle(Comando(Item("C1")), default);

        var resultado = resultados.Should().ContainSingle().Subject;
        resultado.Registrado.Should().BeTrue();
        resultado.Comprobante!.NumeroRecibo.Should().Be(700);
        resultado.Comprobante.Total.Should().Be(137.50m);
        resultado.Comprobante.IdCliente.Should().Be("004-0248-00241");

        var registro = repositorio.Registrados.Should().ContainSingle().Subject;
        registro.FechaSesion.Should().Be(FechaSesion);
        registro.UsuarioResponsable.Should().Be("cajero1");
        registro.Retiro.PagoInteres.Should().Be(12.50m);
    }

    [Fact]
    public async Task Handle_Should_Keep_Other_Contracts_When_One_Fails()
    {
        var repositorio = new RetirosFalsos { FallarEn = "C2" };

        var resultados = await CrearHandler(repositorio).Handle(
            Comando(Item("C1"), Item("C2"), Item("NOEXISTE"), Item("C3")),
            default);

        resultados.Select(r => r.Registrado).Should().Equal(true, false, false, true);
        resultados[1].Error.Should().Be("Estimado usuario, ocurrió un error al momento de retirar el contrato.");
        resultados[2].Error.Should().Contain("no existe o ya fue retirado");
        repositorio.Registrados.Select(r => r.NumeroContrato).Should().Equal("C1", "C3");
        repositorio.Confirmadas.Should().Be(2);
        repositorio.Revertidas.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_Apply_Company_Parameter_For_Advance_Payments()
    {
        var repositorio = new RetirosFalsos
        {
            UltimaFechaPago = new DateTime(2020, 3, 5),
            Sesion = new SesionRetiroDto(FechaSesion, 2, RestarAbonoCapital: false)
        };

        var resultados = await CrearHandler(repositorio).Handle(Comando(Item("C1")), default);

        resultados[0].Comprobante!.PagoInteres.Should().Be(-25m);
        resultados[0].Comprobante!.Total.Should().Be(125m);
    }

    [Fact]
    public async Task Handle_Should_Reject_Withdrawal_When_Company_Has_No_Open_Session()
    {
        var repositorio = new RetirosFalsos { Sesion = new SesionRetiroDto(null, 2, true) };

        var resultados = await CrearHandler(repositorio).Handle(Comando(Item("C1")), default);

        resultados[0].Registrado.Should().BeFalse();
        resultados[0].Error.Should().Contain("sesión abierta");
        repositorio.Registrados.Should().BeEmpty();
    }

    [Fact]
    public void Validator_Should_Reject_Duplicated_Contracts()
    {
        var validator = new RegistrarRetirosCommandValidator();

        validator.Validate(Comando(Item("C1"), Item("C1"))).IsValid.Should().BeFalse();
        validator.Validate(Comando()).IsValid.Should().BeFalse();
        validator.Validate(Comando(Item("C1"), Item("C2"))).IsValid.Should().BeTrue();
    }

    private static RegistrarRetirosCommandHandler CrearHandler(RetirosFalsos repositorio) =>
        new(repositorio, NullLogger<RegistrarRetirosCommandHandler>.Instance);

    private static RegistrarRetirosCommand Comando(params RetiroContratoItem[] items) => new(items, "cajero1");

    private static RetiroContratoItem Item(string numeroContrato) => new("1", 1, numeroContrato);

    private sealed class RetirosFalsos : IRetirosRepository
    {
        private int _numeroRecibo = 699;

        public SesionRetiroDto Sesion { get; init; } = new(FechaSesion, 2, true);
        public DateTime UltimaFechaPago { get; init; } = new(2019, 12, 5);
        public string? FallarEn { get; init; }
        public List<RegistroRetiroDto> Registrados { get; } = [];
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

        public Task<ContratoRetiroDto?> GetContratoBloqueadoAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken) =>
            Task.FromResult(numeroContrato == "NOEXISTE"
                ? null
                : new ContratoRetiroDto(codigoEmpresa, codigoGrupo, numeroContrato, "004-0248-00241",
                    new DateTime(2019, 12, 5), UltimaFechaPago, new DateTime(2020, 6, 5), 125m, 10m));

        public Task<SesionRetiroDto> GetSesionAbiertaAsync(string codigoEmpresa, CancellationToken cancellationToken) =>
            Task.FromResult(Sesion);

        public Task<int> RegistrarRetiroAsync(RegistroRetiroDto registro, CancellationToken cancellationToken)
        {
            if (registro.NumeroContrato == FallarEn)
            {
                throw new InvalidOperationException("Fallo simulado de base de datos.");
            }

            Registrados.Add(registro);
            return Task.FromResult(++_numeroRecibo);
        }
    }
}
