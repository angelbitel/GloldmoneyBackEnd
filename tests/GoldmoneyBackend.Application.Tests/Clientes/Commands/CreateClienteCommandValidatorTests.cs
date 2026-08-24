using FluentAssertions;
using GoldmoneyBackend.Application.Clientes.Commands.CreateCliente;

namespace GoldmoneyBackend.Application.Tests.Clientes.Commands;

public sealed class CreateClienteCommandValidatorTests
{
    private readonly CreateClienteCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_When_Email_Is_Invalid()
    {
        var command = new CreateClienteCommand("Nombre", "email-invalido", "DOC12345");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Succeed_When_Command_Is_Valid()
    {
        var command = new CreateClienteCommand("Nombre", "ok@demo.com", "DOC12345");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
