namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface ISesionesRepository
{
    Task<IReadOnlyList<EstadoSesionEmpresaDto>> GetEstadoSesionesAsync(CancellationToken cancellationToken);
    Task<SesionAbiertaDto> AbrirSesionAsync(AbrirSesionDto dto, CancellationToken cancellationToken);

    /// <summary>Cierre de caja: registra hora, usuario y valor final, pone status 1 y vacía MOVIMIENTO_TEMPORAL.</summary>
    Task<SesionCerradaDto> CerrarSesionAsync(string codigoEmpresa, string usuarioCierre, CancellationToken cancellationToken);

    /// <summary>
    /// sp_Actualizar_Saldos_de_la_Empresa: recalcula saldo_actual e interes_mensual de los contratos de las empresas
    /// que usan la caja indicada. Con <paramref name="aplicar"/> = false solo calcula el impacto.
    /// </summary>
    Task<ActualizacionSaldosDto> ActualizarSaldosAsync(string codigoEmpresaCaja, bool aplicar, CancellationToken cancellationToken);
}

public sealed record ActualizacionSaldosDto(
    string CodigoEmpresaCaja,
    DateTime? FechaSistema,
    bool Aplicado,
    int TotalContratos,
    int ContratosConCambio,
    decimal SaldoAnterior,
    decimal SaldoNuevo,
    IReadOnlyList<ActualizacionSaldosEmpresaDto> Empresas);

public sealed record ActualizacionSaldosEmpresaDto(
    string CodigoEmpresa,
    string? NombreEmpresa,
    int DiasGracia,
    int Contratos,
    int ContratosConCambio,
    decimal SaldoAnterior,
    decimal SaldoNuevo);

public sealed record SesionCerradaDto(
    string CodigoEmpresa,
    DateTime FechaApertura,
    decimal ValorInicialCaja,
    decimal TotalIngresos,
    decimal TotalEgresos,
    decimal ValorFinalCaja,
    DateTime FechaHoraCierre,
    string UsuarioCierre,
    int MovimientosTemporalesEliminados);
