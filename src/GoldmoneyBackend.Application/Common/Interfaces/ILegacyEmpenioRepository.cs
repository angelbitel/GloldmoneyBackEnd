namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface ILegacyEmpenioRepository
{
    Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken);
    Task<EmpresaCajaDto?> GetEmpresaByCodigoAsync(string codigoEmpresa, CancellationToken cancellationToken);
    Task<GrupoEmpenioDto?> GetGrupoAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken);

    /// <summary>Fecha de la sesión abierta de la empresa, o null si no hay una abierta.</summary>
    Task<DateTime?> GetFechaSesionAbiertaAsync(string codigoEmpresa, CancellationToken cancellationToken);

    /// <summary>
    /// Siguiente secuencia del grupo (SECUENCIAS_EMPRESA, con la abreviatura del grupo como código),
    /// incrementada en la propia sentencia para no repetir números con el VB6.
    /// </summary>
    Task<int> SiguienteSecuenciaGrupoAsync(string codigoEmpresa, string abreviaturaGrupo, CancellationToken cancellationToken);

    /// <summary>Resta el capital prestado de la caja en la propia sentencia; falla si la caja no alcanza.</summary>
    Task DescontarCajaAsync(string codigoEmpresa, decimal monto, CancellationToken cancellationToken);
    Task<bool> ContratoExistsAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken);
    Task<ClienteDbDto?> GetClienteByIdAsync(string idCliente, CancellationToken cancellationToken);
    void AddCliente(ClienteDbUpsertDto dto);
    void AddContrato(CrearEmpenioContratoDto dto);
    void AddDetalle(CrearEmpenioDetalleDto detalle, string codigoEmpresa, int codigoGrupo, string numeroContrato, int secuencia);

    /// <summary>Movimiento de caja del empeño (MOVIMIENTO_CAJA y MOVIMIENTO_TEMPORAL), numerado con SEC_CAJA.</summary>
    Task AgregarMovimientoCajaAsync(CrearEmpenioContratoDto dto, string codigoTransaccion, CancellationToken cancellationToken);
    Task<bool> CategoriaPrendaExistsAsync(int codigoCategoriaPrenda, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
