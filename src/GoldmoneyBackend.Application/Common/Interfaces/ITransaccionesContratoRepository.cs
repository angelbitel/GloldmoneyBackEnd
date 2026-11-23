using GoldmoneyBackend.Application.Caja;
using GoldmoneyBackend.Application.Empenios.Transacciones;

namespace GoldmoneyBackend.Application.Common.Interfaces;

/// <summary>
/// Acceso a datos de los procesos de administración de empeño heredados de SADE:
/// adicionar observación, copia de recibo y anulación de contrato.
/// </summary>
public interface ITransaccionesContratoRepository
{
    Task<IReadOnlyList<ContratoTransaccionDatos>> GetContratosPorCedulaAsync(string cedula, CancellationToken cancellationToken);

    Task<ContratoTransaccionDatos?> GetContratoAsync(ContratoClave clave, CancellationToken cancellationToken);

    Task<ParametrosTransaccionDto?> GetParametrosAsync(string codigoEmpresa, CancellationToken cancellationToken);

    /// <summary>Resuelve la empresa que maneja la caja y su sesión abierta; null si no hay sesión abierta.</summary>
    Task<SesionOperacionDto?> GetSesionOperacionAsync(string codigoEmpresa, CancellationToken cancellationToken);

    Task<DateTime> GetFechaServidorAsync(CancellationToken cancellationToken);

    /// <summary>True si el usuario existe, la clave es correcta y tiene permiso efectuar_anulacion.</summary>
    Task<bool> ValidarSupervisorAnulacionAsync(string usuario, string clave, CancellationToken cancellationToken);

    Task<bool> ActualizarObservacionAsync(ContratoClave clave, string? observacion, CancellationToken cancellationToken);

    /// <summary>Registra la copia, suma el monto a la caja y agrega el movimiento CR, todo en una transacción.</summary>
    Task<CopiaReciboRegistradaDto> RegistrarCopiaReciboAsync(RegistrarCopiaReciboDatos datos, CancellationToken cancellationToken);

    /// <summary>Pasa el contrato al historial (AC), devuelve el capital a caja, agrega el movimiento AC y elimina el contrato.</summary>
    Task AnularContratoAsync(AnularContratoDatos datos, CancellationToken cancellationToken);

    /// <summary>Empresa que maneja la caja (EMPRESA.cod_empresa_caja si manejo_caja_dep = 1); null si la empresa no existe.</summary>
    Task<string?> GetEmpresaCajaAsync(string codigoEmpresa, CancellationToken cancellationToken);

    /// <summary>True si la clave es correcta y el usuario es el conectado o un administrador (usuario_admin = 1).</summary>
    Task<bool> ValidarCredencialCajaAsync(string usuarioConectado, string usuario, string clave, CancellationToken cancellationToken);

    /// <summary>Depósito (DD) o retiro (RD) de dinero: movimiento de caja + actualización de EMPRESA.monto_auxiliar.</summary>
    Task<MovimientoCajaRegistradoDto> RegistrarMovimientoManualAsync(MovimientoCajaManualDatos datos, CancellationToken cancellationToken);

    Task<IReadOnlyList<MovimientoCajaRecienteDto>> GetMovimientosRecientesAsync(
        string codigoEmpresaCaja, string tipo, int cantidad, CancellationToken cancellationToken);
}
