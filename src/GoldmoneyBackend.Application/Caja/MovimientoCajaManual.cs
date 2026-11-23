using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Caja;

/// <summary>Códigos de SADE (frmModificacionCaja): DD = depósito de dinero, RD = retiro de dinero.</summary>
public static class TipoMovimientoManual
{
    public const string Deposito = "DD";
    public const string Retiro = "RD";

    public static string Normalizar(string? tipo)
    {
        var valor = tipo?.Trim().ToUpperInvariant();
        return valor is Deposito or Retiro
            ? valor
            : throw new DomainValidationException("Estimado usuario, el tipo de movimiento debe ser depósito (DD) o retiro (RD).");
    }
}

public sealed record MovimientoCajaManualDatos(
    string CodigoEmpresaCaja,
    string Tipo,
    decimal Monto,
    string Motivo,
    DateTime FechaSesion,
    DateTime Hora,
    string UsuarioResponsable);

public sealed record MovimientoCajaRegistradoDto(int NumeroMovimiento, decimal MontoAnterior, decimal MontoNuevo);

public sealed record MovimientoCajaManualResultDto(
    string CodigoEmpresaCaja,
    int NumeroMovimiento,
    string Tipo,
    decimal Monto,
    decimal MontoAnterior,
    decimal MontoNuevo,
    DateTime FechaSesion,
    string UsuarioResponsable);

public sealed record MovimientoCajaRecienteDto(
    int NumeroMovimiento,
    decimal Monto,
    DateTime? Fecha,
    DateTime? Hora,
    string? Motivo,
    string? Usuario);

public sealed record RegistrarMovimientoCajaCommand(
    string CodigoEmpresa,
    string Tipo,
    decimal Monto,
    string Motivo,
    string Usuario,
    string Clave,
    string UsuarioConectado) : IRequest<MovimientoCajaManualResultDto>;

public sealed record MovimientosCajaRecientesQuery(string CodigoEmpresa, string Tipo) : IRequest<IReadOnlyList<MovimientoCajaRecienteDto>>;

public static class ReglasMovimientoCaja
{
    /// <summary>MOVIMIENTO_CAJA.motivo_transaccion es varchar(60).</summary>
    public const int LongitudMotivo = 60;

    public static (decimal Monto, string Motivo) Validar(decimal monto, string? motivo)
    {
        if (monto <= 0)
            throw new DomainValidationException("Estimado usuario, el monto de la transacción debe ser mayor que cero.");

        if (decimal.Round(monto, 2) != monto)
            throw new DomainValidationException("Estimado usuario, el monto solo admite dos decimales.");

        var texto = motivo?.Trim() ?? string.Empty;
        if (texto.Length == 0)
            throw new DomainValidationException("Estimado usuario, no ha ingresado el motivo de la transacción.");

        if (texto.Length > LongitudMotivo)
            throw new DomainValidationException($"Estimado usuario, el motivo admite como máximo {LongitudMotivo} caracteres.");

        return (monto, texto);
    }

    /// <summary>SADE no lo validaba; se evita que la caja quede en negativo.</summary>
    public static void ValidarFondos(string tipo, decimal monto, decimal montoActual)
    {
        if (tipo == TipoMovimientoManual.Retiro && monto > montoActual)
            throw new DomainValidationException(
                $"Estimado usuario, el monto a retirar ({monto:N2}) es mayor que el dinero disponible en caja ({montoActual:N2}).");
    }
}

/// <summary>Equivale a Salvar_Movimniento_Caja de frmModificacionCaja.</summary>
public sealed class RegistrarMovimientoCajaCommandHandler : IRequestHandler<RegistrarMovimientoCajaCommand, MovimientoCajaManualResultDto>
{
    private readonly ITransaccionesContratoRepository _repository;

    public RegistrarMovimientoCajaCommandHandler(ITransaccionesContratoRepository repository)
    {
        _repository = repository;
    }

    public async Task<MovimientoCajaManualResultDto> Handle(RegistrarMovimientoCajaCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoEmpresa))
            throw new DomainValidationException("Estimado usuario, no ha seleccionado una empresa para efectuar la transacción.");

        var tipo = TipoMovimientoManual.Normalizar(request.Tipo);
        var (monto, motivo) = ReglasMovimientoCaja.Validar(request.Monto, request.Motivo);

        var sesion = await _repository.GetSesionOperacionAsync(request.CodigoEmpresa.Trim(), cancellationToken)
            ?? throw new DomainValidationException(
                "Estimado usuario, la caja se encuentra actualmente cerrada, o no ha efectuado un inicio de sesión en el sistema transaccional.");

        // FrmClaveUsuario: la contraseña del usuario conectado o la de un administrador.
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrEmpty(request.Clave)
            || !await _repository.ValidarCredencialCajaAsync(request.UsuarioConectado, request.Usuario.Trim(), request.Clave, cancellationToken))
            throw new DomainValidationException(
                "Estimado usuario, el usuario o la contraseña no son válidos, o el usuario no tiene permiso para modificar la caja.");

        var ahora = await _repository.GetFechaServidorAsync(cancellationToken);
        var responsable = request.Usuario.Trim();
        responsable = responsable.Length > 14 ? responsable[..14] : responsable;

        var registrado = await _repository.RegistrarMovimientoManualAsync(
            new MovimientoCajaManualDatos(sesion.CodigoEmpresaCaja, tipo, monto, motivo, sesion.FechaApertura.Date, ahora, responsable),
            cancellationToken);

        return new MovimientoCajaManualResultDto(
            sesion.CodigoEmpresaCaja, registrado.NumeroMovimiento, tipo, monto, registrado.MontoAnterior, registrado.MontoNuevo,
            sesion.FechaApertura.Date, responsable);
    }
}

/// <summary>Equivale a Cargar_Transacciones_Actuales: últimas 10 transacciones del tipo.</summary>
public sealed class MovimientosCajaRecientesQueryHandler : IRequestHandler<MovimientosCajaRecientesQuery, IReadOnlyList<MovimientoCajaRecienteDto>>
{
    private const int Cantidad = 10;
    private readonly ITransaccionesContratoRepository _repository;

    public MovimientosCajaRecientesQueryHandler(ITransaccionesContratoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<MovimientoCajaRecienteDto>> Handle(MovimientosCajaRecientesQuery request, CancellationToken cancellationToken)
    {
        var tipo = TipoMovimientoManual.Normalizar(request.Tipo);
        var empresaCaja = await _repository.GetEmpresaCajaAsync(request.CodigoEmpresa.Trim(), cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, la empresa seleccionada no existe.");

        return await _repository.GetMovimientosRecientesAsync(empresaCaja, tipo, Cantidad, cancellationToken);
    }
}
