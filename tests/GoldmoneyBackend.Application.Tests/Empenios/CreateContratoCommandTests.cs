using FluentAssertions;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;
using GoldmoneyBackend.Domain.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldmoneyBackend.Application.Tests.Empenios;

public sealed class CreateContratoCommandTests
{
    private static readonly DateTime FechaSesion = new(2026, 11, 8);

    [Fact]
    public async Task Handle_Should_Compute_Everything_From_Session_Group_And_Sequence()
    {
        var repo = new EmpeniosFalsos();

        var resultado = await Crear(repo).Handle(Comando(capital: 150m), default);

        var contrato = repo.Contrato!;
        contrato.NumeroContrato.Should().Be("110000069139");
        contrato.Serie.Should().Be("0000069139");
        contrato.FechaCreacion.Should().Be(FechaSesion);
        contrato.UltimaFechaPago.Should().Be(FechaSesion);
        contrato.FechaVencimiento.Should().Be(new DateTime(2027, 5, 8));
        contrato.TasaInteres.Should().Be(10m);
        contrato.InteresMensual.Should().Be(15m);
        contrato.PlazoMeses.Should().Be(6);
        contrato.UsuarioResponsable.Should().Be("soporte");

        repo.SecuenciaPedida.Should().Be(("1", "ORO"));
        repo.CajaDescontada.Should().Be(150m);
        repo.Movimiento.Should().Be(("EN", contrato));
        repo.Detalles.Should().Equal((1, 7), (2, 7));
        repo.Guardado.Should().BeTrue();

        resultado.ContratoId.Should().Be("11110000069139");
        resultado.NumeroContrato.Should().Be("110000069139");
        resultado.FechaVencimiento.Should().Be(new DateTime(2027, 5, 8));
    }

    [Fact]
    public async Task Handle_Should_Skip_Numbers_Already_Used()
    {
        var repo = new EmpeniosFalsos { NumerosOcupados = ["110000069139", "110000069140"] };

        await Crear(repo).Handle(Comando(), default);

        repo.Contrato!.NumeroContrato.Should().Be("110000069141");
    }

    [Fact]
    public async Task Handle_Should_Reject_When_There_Is_No_Open_Session()
    {
        var repo = new EmpeniosFalsos { FechaSesion = null };

        var accion = () => Crear(repo).Handle(Comando(), default);

        await accion.Should().ThrowAsync<DomainValidationException>().WithMessage("*sesión de caja abierta*");
        repo.Guardado.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Reject_Inactive_Group()
    {
        var repo = new EmpeniosFalsos { Grupo = GrupoOro with { Activo = false } };

        var accion = () => Crear(repo).Handle(Comando(), default);

        await accion.Should().ThrowAsync<DomainValidationException>().WithMessage("*grupo*");
    }

    [Fact]
    public async Task Handle_Should_Reject_When_Cash_Is_Not_Enough()
    {
        var repo = new EmpeniosFalsos { Caja = 100m };

        var accion = () => Crear(repo).Handle(Comando(capital: 150m), default);

        await accion.Should().ThrowAsync<DomainValidationException>().WithMessage("*caja*");
        repo.Guardado.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Require_Name_To_Register_A_New_Client()
    {
        var repo = new EmpeniosFalsos { ClienteExiste = false };

        var sinNombre = () => Crear(repo).Handle(Comando() with { Nombre = null }, default);
        await sinNombre.Should().ThrowAsync<DomainValidationException>().WithMessage("*nombre y apellido*");

        await Crear(repo).Handle(Comando(), default);
        repo.ClienteAgregado!.Nombre.Should().Be("Corrine");
    }

    [Theory]
    [InlineData(0, 18, false)]       // capital en cero
    [InlineData(150.005, 18, false)] // más de dos decimales
    [InlineData(150, 100, false)]    // kilates: DETALLES_CONTRATOS.kilates es decimal(2,0)
    [InlineData(150, 18.5, false)]
    [InlineData(150, 18, true)]
    public void Validator_Should_Respect_Legacy_Columns(double capital, double kilates, bool valido)
    {
        var comando = Comando((decimal)capital) with
        {
            Detalles = [new CreateContratoDetalleCommand(7, "Cadena", 12.5m, (decimal)kilates, 1)]
        };

        new CreateContratoCommandValidator().Validate(comando).IsValid.Should().Be(valido);
    }

    private static readonly GrupoEmpenioDto GrupoOro = new("1", 1, "ORO", 10m, 6, true);

    private static CreateContratoCommandHandler Crear(EmpeniosFalsos repo) =>
        new(repo, NullLogger<CreateContratoCommandHandler>.Instance);

    private static CreateContratoCommand Comando(decimal capital = 150m) => new(
        "1", 1, "001-0011-00314", "Corrine", "Gallimore", null, null, "001", capital,
        [new CreateContratoDetalleCommand(7, "Cadena", 12.5m, 18m, 1), new CreateContratoDetalleCommand(7, "Anillo", 3m, 14m, 1)],
        "soporte");

    private sealed class EmpeniosFalsos : ILegacyEmpenioRepository
    {
        private int _secuencia = 69138;

        public DateTime? FechaSesion { get; init; } = CreateContratoCommandTests.FechaSesion;
        public GrupoEmpenioDto? Grupo { get; init; } = GrupoOro;
        public decimal? Caja { get; init; } = 10_000m;
        public bool ClienteExiste { get; init; } = true;
        public HashSet<string> NumerosOcupados { get; init; } = [];

        public (string, string)? SecuenciaPedida { get; private set; }
        public decimal? CajaDescontada { get; private set; }
        public CrearEmpenioContratoDto? Contrato { get; private set; }
        public ClienteDbUpsertDto? ClienteAgregado { get; private set; }
        public List<(int Secuencia, int Prenda)> Detalles { get; } = [];
        public (string, CrearEmpenioContratoDto)? Movimiento { get; private set; }
        public bool Guardado { get; private set; }

        public Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken) => operacion(cancellationToken);
        public Task<EmpresaCajaDto?> GetEmpresaByCodigoAsync(string codigoEmpresa, CancellationToken cancellationToken) =>
            Task.FromResult<EmpresaCajaDto?>(new EmpresaCajaDto(codigoEmpresa, 0m, Caja, null));
        public Task<GrupoEmpenioDto?> GetGrupoAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken) => Task.FromResult(Grupo);
        public Task<DateTime?> GetFechaSesionAbiertaAsync(string codigoEmpresa, CancellationToken cancellationToken) => Task.FromResult(FechaSesion);

        public Task<int> SiguienteSecuenciaGrupoAsync(string codigoEmpresa, string abreviaturaGrupo, CancellationToken cancellationToken)
        {
            SecuenciaPedida = (codigoEmpresa, abreviaturaGrupo);
            return Task.FromResult(++_secuencia);
        }

        public Task DescontarCajaAsync(string codigoEmpresa, decimal monto, CancellationToken cancellationToken)
        {
            CajaDescontada = monto;
            return Task.CompletedTask;
        }

        public Task<bool> ContratoExistsAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken) =>
            Task.FromResult(NumerosOcupados.Contains(numeroContrato));

        public Task<ClienteDbDto?> GetClienteByIdAsync(string idCliente, CancellationToken cancellationToken) =>
            Task.FromResult(ClienteExiste ? new ClienteDbDto(idCliente, null, null, null, 1, null, null, null, null, null, null) : null);

        public void AddCliente(ClienteDbUpsertDto dto) => ClienteAgregado = dto;
        public void AddContrato(CrearEmpenioContratoDto dto) => Contrato = dto;

        public void AddDetalle(CrearEmpenioDetalleDto detalle, string codigoEmpresa, int codigoGrupo, string numeroContrato, int secuencia) =>
            Detalles.Add((secuencia, detalle.CodigoTipoPrenda));

        public Task AgregarMovimientoCajaAsync(CrearEmpenioContratoDto dto, string codigoTransaccion, CancellationToken cancellationToken)
        {
            Movimiento = (codigoTransaccion, dto);
            return Task.CompletedTask;
        }

        public Task<bool> CategoriaPrendaExistsAsync(int codigoCategoriaPrenda, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            Guardado = true;
            return Task.FromResult(1);
        }
    }
}
