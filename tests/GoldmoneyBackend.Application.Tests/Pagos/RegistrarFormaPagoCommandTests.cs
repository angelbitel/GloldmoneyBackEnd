using FluentAssertions;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Pagos.Commands.RegistrarFormaPago;

namespace GoldmoneyBackend.Application.Tests.Pagos;

public sealed class RegistrarFormaPagoCommandTests
{
    private static readonly DocumentoCobroDto Abono = new("ABONO", "11", 1, "110000068815", 501, 60m);
    private static readonly DocumentoCobroDto Retiro = new("RETIRO", "11", 1, "110000068816", 77, 40m);

    [Fact]
    public void Validator_Should_Accept_Combined_Payment()
    {
        var resultado = Validar([new("efectivo", 70m), new("yappy", 30m)], [Abono, Retiro]);

        resultado.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("cheque", 100)]   // método desconocido
    [InlineData("Efectivo", 100)] // los métodos van en minúscula, como los envía el frontend
    [InlineData("efectivo", 0)]
    [InlineData("efectivo", 10.005)]
    public void Validator_Should_Reject_Invalid_Method(string metodo, double monto)
    {
        Validar([new(metodo, (decimal)monto)], [Abono]).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Reject_Repeated_Method_Or_Receipt_And_Empty_Lists()
    {
        Validar([new("efectivo", 50m), new("efectivo", 50m)], [Abono]).IsValid.Should().BeFalse();
        Validar([new("efectivo", 120m)], [Abono, Abono]).IsValid.Should().BeFalse();
        Validar([], [Abono]).IsValid.Should().BeFalse();
        Validar([new("efectivo", 60m)], []).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Reject_Unknown_Document_Type()
    {
        Validar([new("efectivo", 60m)], [Abono with { Tipo = "PAGO" }]).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_Should_Trim_Keys_And_Pass_The_Responsible_User()
    {
        var repositorio = new CobrosFalsos();
        var command = new RegistrarFormaPagoCommand(
            " 8-923-441 ",
            [new("efectivo", 60m)],
            [Abono with { CodigoEmpresa = "11 ", NumeroContrato = " 110000068815" }],
            "cajero1");

        await new RegistrarFormaPagoCommandHandler(repositorio).Handle(command, default);

        repositorio.Registrado!.IdCliente.Should().Be("8-923-441");
        repositorio.Registrado.Usuario.Should().Be("cajero1");
        repositorio.Registrado.Documentos.Should().ContainSingle()
            .Which.Should().Be(Abono);
    }

    private static FluentValidation.Results.ValidationResult Validar(
        IReadOnlyList<MetodoPagoCobroDto> metodos,
        IReadOnlyList<DocumentoCobroDto> documentos) =>
        new RegistrarFormaPagoCommandValidator().Validate(new RegistrarFormaPagoCommand("8-923-441", metodos, documentos, "cajero1"));

    private sealed class CobrosFalsos : ICobrosRepository
    {
        public RegistroCobroDto? Registrado { get; private set; }

        public Task<CobroDto> RegistrarAsync(RegistroCobroDto cobro, CancellationToken cancellationToken)
        {
            Registrado = cobro;
            return Task.FromResult(new CobroDto(
                Guid.NewGuid(), cobro.FechaUtc, cobro.Usuario, cobro.IdCliente,
                cobro.Metodos.Sum(x => x.Monto), cobro.Documentos.Sum(x => x.Total), cobro.Metodos, cobro.Documentos));
        }

        public Task<CobroDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
