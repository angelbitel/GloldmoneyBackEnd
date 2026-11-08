using FluentAssertions;
using GoldmoneyBackend.Application.Clientes.Commands.ActualizarComentarioCliente;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Application.Tests.Clientes.Commands;

public sealed class ActualizarComentarioClienteCommandTests
{
    [Theory]
    [InlineData("  nota  ", "nota")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public async Task Handler_Should_Trim_And_Clear_Empty_Notes(string? comentario, string? guardado)
    {
        var clientes = new ClientesFalsos { Existe = true };

        await new ActualizarComentarioClienteCommandHandler(clientes)
            .Handle(new ActualizarComentarioClienteCommand(" 8-923-441 ", comentario), default);

        clientes.IdCliente.Should().Be("8-923-441");
        clientes.Comentario.Should().Be(guardado);
    }

    [Fact]
    public async Task Handler_Should_Throw_NotFound_When_Client_Does_Not_Exist()
    {
        var handler = new ActualizarComentarioClienteCommandHandler(new ClientesFalsos { Existe = false });

        var accion = () => handler.Handle(new ActualizarComentarioClienteCommand("8-0-0", "nota"), default);

        await accion.Should().ThrowAsync<NotFoundDomainException>();
    }

    [Fact]
    public void Validator_Should_Respect_Legacy_Column_Size()
    {
        var validador = new ActualizarComentarioClienteCommandValidator();

        validador.Validate(new ActualizarComentarioClienteCommand("8-923-441", new string('x', 300))).IsValid.Should().BeTrue();
        validador.Validate(new ActualizarComentarioClienteCommand("8-923-441", new string('x', 301))).IsValid.Should().BeFalse();
    }

    private sealed class ClientesFalsos : IClientesRepository
    {
        public bool Existe { get; init; }
        public string? IdCliente { get; private set; }
        public string? Comentario { get; private set; }

        public Task<bool> ActualizarComentarioAsync(string idCliente, string? comentario, CancellationToken cancellationToken)
        {
            IdCliente = idCliente;
            Comentario = comentario;
            return Task.FromResult(Existe);
        }

        public Task<IReadOnlyList<ClienteDbDto>> GetAllAsync(string? search, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task CreateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task UpdateAsync(ClienteDbUpsertDto dto, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
