using FluentAssertions;
using GoldmoneyBackend.Application.Auth;
using GoldmoneyBackend.Application.Auth.Commands.RestablecerContrasena;
using GoldmoneyBackend.Application.Auth.Commands.SolicitarRestablecimientoContrasena;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldmoneyBackend.Application.Tests.Auth;

public sealed class RestablecimientoContrasenaTests
{
    [Fact]
    public void Token_Should_Be_Url_Safe_Unique_And_Stored_Only_As_Hash()
    {
        var token = TokenRestablecimiento.Generar();
        var otro = TokenRestablecimiento.Generar();

        token.Should().MatchRegex("^[A-Za-z0-9_-]{43}$");
        token.Should().NotBe(otro);
        TokenRestablecimiento.Hash(token).Should().MatchRegex("^[0-9a-f]{64}$").And.NotContain(token);
        TokenRestablecimiento.Hash(token).Should().Be(TokenRestablecimiento.Hash(token));
    }

    [Fact]
    public async Task Solicitar_Should_Send_Link_And_Store_Hash_When_Account_Has_Email()
    {
        var cuentas = new CuentasFalsas { Cuenta = new UsuarioCorreoDto("cajero1", "cajero@demo.com") };
        var notificador = new NotificadorFalso();

        await CrearHandler(cuentas, notificador).Handle(new SolicitarRestablecimientoContrasenaCommand("cajero1"), default);

        notificador.Enviados.Should().ContainSingle();
        var (correo, token) = notificador.Enviados[0];
        correo.Should().Be("cajero@demo.com");
        cuentas.HashRegistrado.Should().Be(TokenRestablecimiento.Hash(token));
        (cuentas.ExpiraRegistrado - cuentas.CreadoRegistrado).Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task Solicitar_Should_Do_Nothing_When_Account_Has_No_Email()
    {
        var cuentas = new CuentasFalsas { Cuenta = null };
        var notificador = new NotificadorFalso();

        await CrearHandler(cuentas, notificador).Handle(new SolicitarRestablecimientoContrasenaCommand("noexiste"), default);

        notificador.Enviados.Should().BeEmpty();
        cuentas.HashRegistrado.Should().BeNull();
    }

    [Fact]
    public async Task Solicitar_Should_Stop_After_Three_Requests_In_One_Hour()
    {
        var cuentas = new CuentasFalsas { Cuenta = new UsuarioCorreoDto("cajero1", "cajero@demo.com"), SolicitudesRecientes = 3 };
        var notificador = new NotificadorFalso();

        await CrearHandler(cuentas, notificador).Handle(new SolicitarRestablecimientoContrasenaCommand("cajero1"), default);

        notificador.Enviados.Should().BeEmpty();
    }

    [Fact]
    public async Task Restablecer_Should_Throw_When_Link_Is_Invalid_Or_Expired()
    {
        var handler = new RestablecerContrasenaCommandHandler(new CuentasFalsas { RestablecerResultado = false });

        var accion = () => handler.Handle(new RestablecerContrasenaCommand("token", "nueva123"), default);

        await accion.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task Restablecer_Should_Look_Up_The_Hash_Of_The_Token()
    {
        var cuentas = new CuentasFalsas { RestablecerResultado = true };

        await new RestablecerContrasenaCommandHandler(cuentas).Handle(new RestablecerContrasenaCommand(" abc ", "nueva123"), default);

        cuentas.HashConsultado.Should().Be(TokenRestablecimiento.Hash("abc"));
        cuentas.ContrasenaGuardada.Should().Be("nueva123");
    }

    [Theory]
    [InlineData("12345", false)]                     // menos de 6
    [InlineData("123456", true)]
    [InlineData("1234567890123456789012345", true)]  // 25: tamaño de USUARIOS.contrasena
    [InlineData("12345678901234567890123456", false)]
    [InlineData(" clave1", false)]
    public void Validator_Should_Respect_Legacy_Password_Column(string contrasena, bool valida)
    {
        var resultado = new RestablecerContrasenaCommandValidator().Validate(new RestablecerContrasenaCommand("token", contrasena));

        resultado.IsValid.Should().Be(valida);
    }

    private static SolicitarRestablecimientoContrasenaCommandHandler CrearHandler(CuentasFalsas cuentas, NotificadorFalso notificador) =>
        new(cuentas, notificador, NullLogger<SolicitarRestablecimientoContrasenaCommandHandler>.Instance);

    private sealed class NotificadorFalso : INotificadorCuentas
    {
        public List<(string Correo, string Token)> Enviados { get; } = [];

        public Task EnviarEnlaceRestablecimientoAsync(string correo, string nombreUsuario, string token, DateTime expiraEnUtc, CancellationToken cancellationToken)
        {
            Enviados.Add((correo, token));
            return Task.CompletedTask;
        }
    }

    private sealed class CuentasFalsas : ICuentasUsuarioService
    {
        public UsuarioCorreoDto? Cuenta { get; init; }
        public int SolicitudesRecientes { get; init; }
        public bool RestablecerResultado { get; init; }
        public string? HashRegistrado { get; private set; }
        public DateTime CreadoRegistrado { get; private set; }
        public DateTime ExpiraRegistrado { get; private set; }
        public string? HashConsultado { get; private set; }
        public string? ContrasenaGuardada { get; private set; }

        public Task<UsuarioCorreoDto?> BuscarPorUsuarioOCorreoAsync(string usuarioOCorreo, CancellationToken cancellationToken) =>
            Task.FromResult(Cuenta);

        public Task<int> ContarSolicitudesDesdeAsync(string nombreUsuario, DateTime desdeUtc, CancellationToken cancellationToken) =>
            Task.FromResult(SolicitudesRecientes);

        public Task RegistrarSolicitudAsync(string nombreUsuario, string tokenHash, DateTime creadoEnUtc, DateTime expiraEnUtc, CancellationToken cancellationToken)
        {
            HashRegistrado = tokenHash;
            CreadoRegistrado = creadoEnUtc;
            ExpiraRegistrado = expiraEnUtc;
            return Task.CompletedTask;
        }

        public Task<bool> RestablecerContrasenaAsync(string tokenHash, string nuevaContrasena, DateTime ahoraUtc, CancellationToken cancellationToken)
        {
            HashConsultado = tokenHash;
            ContrasenaGuardada = nuevaContrasena;
            return Task.FromResult(RestablecerResultado);
        }

        public Task<IReadOnlyDictionary<string, string>> GetCorreosAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> UsuarioExisteAsync(string nombreUsuario, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task GuardarCorreoAsync(string nombreUsuario, string? correo, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
