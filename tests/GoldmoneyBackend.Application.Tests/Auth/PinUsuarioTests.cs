using FluentAssertions;
using GoldmoneyBackend.Application.Auth.Commands.AsignarPinUsuario;
using GoldmoneyBackend.Application.Auth.Commands.CambiarMiPin;
using GoldmoneyBackend.Application.Auth.DTOs;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;
using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Application.Tests.Auth;

public sealed class PinUsuarioTests
{
    [Theory]
    [InlineData("1234", true)]
    [InlineData("123456", true)]
    [InlineData("123", false)]       // menos de 4
    [InlineData("1234567", false)]   // más de 6
    [InlineData("12a4", false)]      // solo dígitos
    [InlineData("", false)]
    public void Validator_Should_Accept_Only_Four_To_Six_Digits(string pin, bool valido)
    {
        new AsignarPinUsuarioCommandValidator().Validate(new AsignarPinUsuarioCommand("mary", pin)).IsValid.Should().Be(valido);
        new CambiarMiPinCommandValidator().Validate(new CambiarMiPinCommand("mary", "clave", pin)).IsValid.Should().Be(valido);
    }

    [Fact]
    public async Task Asignar_Should_Throw_NotFound_When_User_Does_Not_Exist()
    {
        var handler = new AsignarPinUsuarioCommandHandler(new CuentasFalsas { UsuarioExiste = false });

        var accion = () => handler.Handle(new AsignarPinUsuarioCommand("noexiste", "1234"), default);

        await accion.Should().ThrowAsync<NotFoundDomainException>();
    }

    [Fact]
    public async Task CambiarMiPin_Should_Require_The_Current_Password()
    {
        var cuentas = new CuentasFalsas();
        var handler = new CambiarMiPinCommandHandler(new AuthFalso("clave-buena"), cuentas);

        var conClaveMala = () => handler.Handle(new CambiarMiPinCommand("mary", "clave-mala", "1234"), default);
        await conClaveMala.Should().ThrowAsync<DomainValidationException>().WithMessage("*contraseña*");
        cuentas.PinGuardado.Should().BeNull();

        await handler.Handle(new CambiarMiPinCommand(" mary ", "clave-buena", "1234"), default);
        cuentas.PinGuardado.Should().Be(("mary", "1234"));
    }

    [Theory]
    [InlineData(ResultadoPin.SinPin, "*no tiene PIN*")]
    [InlineData(ResultadoPin.Incorrecto, "*PIN no es correcto*")]
    public async Task AbrirSesion_Should_Not_Open_Without_A_Valid_Pin(ResultadoPin resultado, string mensaje)
    {
        var sesiones = new SesionesFalsas();
        var handler = new AbrirSesionCommandHandler(sesiones, new CuentasFalsas { ResultadoVerificacion = resultado });

        var accion = () => handler.Handle(new AbrirSesionCommand("1", "soporte", "0000"), default);

        await accion.Should().ThrowAsync<DomainValidationException>().WithMessage(mensaje);
        sesiones.Abierta.Should().BeNull();
    }

    [Fact]
    public async Task AbrirSesion_Should_Open_With_A_Valid_Pin()
    {
        var sesiones = new SesionesFalsas();
        var handler = new AbrirSesionCommandHandler(sesiones, new CuentasFalsas { ResultadoVerificacion = ResultadoPin.Correcto });

        await handler.Handle(new AbrirSesionCommand(" 1 ", " soporte ", "1234"), default);

        sesiones.Abierta.Should().Be(new AbrirSesionDto("1", "soporte"));
    }

    private sealed class SesionesFalsas : ISesionesRepository
    {
        public AbrirSesionDto? Abierta { get; private set; }

        public Task<SesionAbiertaDto> AbrirSesionAsync(AbrirSesionDto dto, CancellationToken cancellationToken)
        {
            Abierta = dto;
            return Task.FromResult(new SesionAbiertaDto(dto.CodigoEmpresa, new DateTime(2026, 11, 8), 100m));
        }

        public Task<IReadOnlyList<EstadoSesionEmpresaDto>> GetEstadoSesionesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class AuthFalso(string claveCorrecta) : IAuthService
    {
        public Task<AuthTokenDto?> LoginAsync(string userName, string password, CancellationToken cancellationToken) =>
            Task.FromResult(password == claveCorrecta ? new AuthTokenDto("token", DateTime.UtcNow.AddHours(1), userName, ["Analyst"]) : null);

        public Task<IReadOnlyList<UsuarioDto>> GetUsuariosAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class CuentasFalsas : ICuentasUsuarioService
    {
        public bool UsuarioExiste { get; init; } = true;
        public ResultadoPin ResultadoVerificacion { get; init; }
        public (string Usuario, string Pin)? PinGuardado { get; private set; }

        public Task<bool> GuardarPinAsync(string nombreUsuario, string pin, DateTime ahoraUtc, CancellationToken cancellationToken)
        {
            if (UsuarioExiste) PinGuardado = (nombreUsuario, pin);
            return Task.FromResult(UsuarioExiste);
        }

        public Task<ResultadoPin> VerificarPinAsync(string nombreUsuario, string pin, CancellationToken cancellationToken) =>
            Task.FromResult(ResultadoVerificacion);

        public Task<IReadOnlySet<string>> GetUsuariosConPinAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, string>> GetCorreosAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> UsuarioExisteAsync(string nombreUsuario, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task GuardarCorreoAsync(string nombreUsuario, string? correo, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<UsuarioCorreoDto?> BuscarPorUsuarioOCorreoAsync(string usuarioOCorreo, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> ContarSolicitudesDesdeAsync(string nombreUsuario, DateTime desdeUtc, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RegistrarSolicitudAsync(string nombreUsuario, string tokenHash, DateTime creadoEnUtc, DateTime expiraEnUtc, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RestablecerContrasenaAsync(string tokenHash, string nuevaContrasena, DateTime ahoraUtc, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> CambiarContrasenaAsync(string nombreUsuario, string nuevaContrasena, DateTime ahoraUtc, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
