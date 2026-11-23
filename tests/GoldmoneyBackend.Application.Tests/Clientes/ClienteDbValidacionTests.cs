using FluentAssertions;
using GoldmoneyBackend.Application.Clientes;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Application.Tests.Clientes;

public sealed class ClienteDbValidacionTests
{
    private static ClienteDbUpsertDto Cliente(
        string id = "008-0481-00391", string? nombre = "Javier", string? apellido = "Oses",
        string? direccion = "Calle 50", int? estatus = 1) =>
        new(id, apellido, nombre, "6000-0000", estatus, direccion, null, "001", null, null, null);

    [Fact]
    public void Cliente_Valido_No_Lanza()
    {
        var act = () => ClienteDbValidacion.Validar(Cliente());
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("", "Javier", "Oses", "cédula")]
    [InlineData("8-1-1", "", "Oses", "nombre")]
    [InlineData("8-1-1", "Javier", " ", "apellido")]
    public void Campos_Obligatorios(string id, string nombre, string apellido, string esperado)
    {
        var act = () => ClienteDbValidacion.Validar(Cliente(id, nombre, apellido));
        act.Should().Throw<DomainValidationException>().WithMessage($"*{esperado}*");
    }

    [Fact]
    public void Respeta_Longitud_De_Columnas()
    {
        var act = () => ClienteDbValidacion.Validar(Cliente(direccion: new string('x', 41)));
        act.Should().Throw<DomainValidationException>().WithMessage("*40 caracteres*");
    }

    [Fact]
    public void Estatus_Solo_Cero_O_Uno()
    {
        var act = () => ClienteDbValidacion.Validar(Cliente(estatus: 5));
        act.Should().Throw<DomainValidationException>();
    }
}
