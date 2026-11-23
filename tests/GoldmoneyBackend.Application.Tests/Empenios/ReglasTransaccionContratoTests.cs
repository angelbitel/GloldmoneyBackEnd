using FluentAssertions;
using GoldmoneyBackend.Application.Empenios.Transacciones;
using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Application.Tests.Empenios;

public sealed class ReglasTransaccionContratoTests
{
    private static readonly DateTime FechaSesion = new(2026, 11, 16);
    private static readonly SesionOperacionDto Sesion = new("1", FechaSesion);
    private static readonly ParametrosTransaccionDto SinControles = new(2.50m, false, false, null);

    [Fact]
    public void Contrato_Del_Dia_Sin_Pagos_Es_Anulable()
    {
        var motivo = ReglasTransaccionContrato.MotivoNoAnulable(Contrato(), Sesion, SinControles, FechaSesion.AddHours(15));

        motivo.Should().BeNull();
    }

    [Fact]
    public void Sin_Sesion_Abierta_No_Es_Anulable()
    {
        ReglasTransaccionContrato.MotivoNoAnulable(Contrato(), null, SinControles, FechaSesion)
            .Should().Contain("sesión");
    }

    [Fact]
    public void Contrato_De_Otra_Fecha_No_Es_Anulable()
    {
        var contrato = Contrato(fechaCreacion: FechaSesion.AddDays(-1));

        ReglasTransaccionContrato.MotivoNoAnulable(contrato, Sesion, SinControles, FechaSesion)
            .Should().Contain("solo se anulan contratos del día");
    }

    [Fact]
    public void Contrato_Con_Pagos_No_Es_Anulable()
    {
        var contrato = Contrato(tienePagos: true);

        ReglasTransaccionContrato.MotivoNoAnulable(contrato, Sesion, SinControles, FechaSesion)
            .Should().Contain("abonos a interés");
    }

    [Theory]
    [InlineData(10, 25, false)] // creado 10:00, 25 min después, límite 30 → anulable
    [InlineData(10, 31, true)]  // 31 min después → fuera de tiempo
    public void Controla_Tiempo_De_Anulacion(int hora, int minutosDespues, bool fueraDeTiempo)
    {
        var parametros = new ParametrosTransaccionDto(1m, false, true, 30);
        var creado = FechaSesion.AddHours(hora);

        ReglasTransaccionContrato.SobrepasaTiempoAnulacion(creado, parametros, creado.AddMinutes(minutosDespues))
            .Should().Be(fueraDeTiempo);
    }

    [Fact]
    public void Tiempo_No_Se_Controla_Si_El_Parametro_Esta_Apagado()
    {
        var creado = FechaSesion.AddHours(8);

        ReglasTransaccionContrato.SobrepasaTiempoAnulacion(creado, SinControles, creado.AddHours(10))
            .Should().BeFalse();
    }

    [Fact]
    public void ValidarAnulacion_Lanza_Mensaje_Para_El_Usuario()
    {
        var act = () => ReglasTransaccionContrato.ValidarAnulacion(Contrato(tienePagos: true), Sesion, SinControles, FechaSesion);

        act.Should().Throw<DomainValidationException>().WithMessage("Estimado usuario, el contrato*");
    }

    [Fact]
    public void Costo_De_Copia_Es_Obligatorio()
    {
        var act = () => ReglasTransaccionContrato.CostoCopia(new ParametrosTransaccionDto(null, false, false, null));

        act.Should().Throw<DomainValidationException>();
        ReglasTransaccionContrato.CostoCopia(SinControles).Should().Be(2.50m);
    }

    [Fact]
    public void Observacion_Se_Recorta_Y_Valida_Longitud()
    {
        ReglasTransaccionContrato.NormalizarObservacion("  hola  ").Should().Be("hola");
        ReglasTransaccionContrato.NormalizarObservacion("   ").Should().BeNull();

        var act = () => ReglasTransaccionContrato.NormalizarObservacion(new string('x', 81));
        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData(0, "Original")]
    [InlineData(2, "Copia")]
    public void Estado_Depende_De_Las_Copias(int copias, string estado)
    {
        ReglasTransaccionContrato.Estado(copias).Should().Be(estado);
    }

    [Fact]
    public void Administracion_Resume_Estado_Y_Anulacion()
    {
        var parametros = new ParametrosTransaccionDto(3m, true, false, null);
        var contrato = Contrato() with { CantidadCopias = 1, NombreEmpresa = " Más Plata ", AbreviaturaGrupo = "oro " };

        var a = AdministracionContrato.Construir(contrato, parametros, Sesion, FechaSesion.AddHours(12));

        a.Estado.Should().Be("Copia");
        a.PuedeAnular.Should().BeTrue();
        a.MotivoNoAnulable.Should().BeNull();
        a.RequiereSupervisor.Should().BeTrue();
        a.CostoCopia.Should().Be(3m);
        a.NombreEmpresa.Should().Be("Más Plata");
        a.AbreviaturaGrupo.Should().Be("ORO");
        a.FechaSesion.Should().Be(FechaSesion);
    }

    [Fact]
    public void Administracion_Sin_Sesion_No_Permite_Anular()
    {
        var a = AdministracionContrato.Construir(Contrato(), SinControles, null, FechaSesion);

        a.PuedeAnular.Should().BeFalse();
        a.MotivoNoAnulable.Should().Contain("sesión");
        a.FechaSesion.Should().BeNull();
    }

    private static ContratoTransaccionDatos Contrato(DateTime? fechaCreacion = null, bool tienePagos = false) =>
        new("1", "Empresa", 1, "A", "11000001", "0000001", "8-888-888", 100m,
            fechaCreacion ?? FechaSesion, 10m, 4, FechaSesion.AddMonths(4), 10m, 100m,
            null, FechaSesion.AddHours(10), "admin", 0, tienePagos, []);
}
