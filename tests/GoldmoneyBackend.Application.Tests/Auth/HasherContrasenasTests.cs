using FluentAssertions;
using GoldmoneyBackend.Infrastructure.Authentication;
using GoldmoneyBackend.Infrastructure.Authentication.Models;
using Microsoft.Extensions.Options;

namespace GoldmoneyBackend.Application.Tests.Auth;

public sealed class HasherContrasenasTests
{
    private const string Pepper = "pepper-de-prueba-de-32-caracteres!!";

    [Fact]
    public void Hash_Should_Verify_Same_Password_Only_And_Use_Random_Salt()
    {
        var hasher = Crear(Pepper);

        var hash = hasher.Hash("clave-segura-1");

        hash.Should().StartWith("pbkdf2-sha256$100000$p$").And.NotContain("clave-segura-1");
        hasher.Verificar("clave-segura-1", hash).Should().BeTrue();
        hasher.Verificar("clave-segura-2", hash).Should().BeFalse();
        hasher.Hash("clave-segura-1").Should().NotBe(hash);
    }

    [Fact]
    public void Verificar_Should_Fail_When_Pepper_Changes_Or_Is_Missing()
    {
        var hash = Crear(Pepper).Hash("clave-segura-1");

        Crear("otro-pepper-de-prueba-de-32-caracteres").Verificar("clave-segura-1", hash).Should().BeFalse();
        Crear(string.Empty).Verificar("clave-segura-1", hash).Should().BeFalse();
    }

    [Fact]
    public void NecesitaRehacer_Should_Detect_Fewer_Iterations_Or_Pepper_Change()
    {
        var sinPepper = Crear(string.Empty).Hash("clave-segura-1");
        var conPepper = Crear(Pepper).Hash("clave-segura-1");

        Crear(Pepper).NecesitaRehacer(conPepper).Should().BeFalse();
        Crear(Pepper).NecesitaRehacer(sinPepper).Should().BeTrue();
        Crear(Pepper, iteraciones: 200_000).NecesitaRehacer(conPepper).Should().BeTrue();

        // Un hash sin pepper sigue sirviendo después de configurar uno; al iniciar sesión se rehace.
        Crear(Pepper).Verificar("clave-segura-1", sinPepper).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto-plano")]
    [InlineData("pbkdf2$sha256$100000$c2Fs$aGFzaA==")]
    [InlineData("pbkdf2-sha256$100000$x$c2Fs$aGFzaA==")]
    public void Verificar_Should_Reject_Unknown_Formats(string almacenado)
    {
        Crear(Pepper).Verificar(almacenado, almacenado).Should().BeFalse();
    }

    [Theory]
    [InlineData(50_000, "")]
    [InlineData(600_000, "pepper-corto")]
    public void Constructor_Should_Reject_Weak_Settings(int iteraciones, string pepper)
    {
        var crear = () => Crear(pepper, iteraciones);

        crear.Should().Throw<InvalidOperationException>();
    }

    private static HasherContrasenas Crear(string pepper, int iteraciones = 100_000) =>
        new(Options.Create(new ContrasenasOptions { Iteraciones = iteraciones, Pepper = pepper }));
}
