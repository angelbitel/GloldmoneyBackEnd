using FluentAssertions;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Domain.Entities.Clientes;
using GoldmoneyBackend.Domain.ValueObjects;

namespace GoldmoneyBackend.Domain.Tests.Entities;

public sealed class ClienteTests
{
    [Fact]
    public void Create_Should_Create_Cliente_When_Data_Is_Valid()
    {
        var email = Email.Create("cliente@demo.com");
        var documento = Documento.Create("DOC12345");

        var cliente = Cliente.Create("Juan Perez", email, documento);

        cliente.Id.Should().NotBeEmpty();
        cliente.Nombre.Should().Be("Juan Perez");
        cliente.Email.Value.Should().Be("cliente@demo.com");
        cliente.Documento.Value.Should().Be("DOC12345");
    }

    [Fact]
    public void Create_Should_Throw_When_Nombre_Is_Empty()
    {
        var email = Email.Create("cliente@demo.com");
        var documento = Documento.Create("DOC12345");

        var action = () => Cliente.Create(string.Empty, email, documento);

        action.Should().Throw<DomainValidationException>();
    }
}
