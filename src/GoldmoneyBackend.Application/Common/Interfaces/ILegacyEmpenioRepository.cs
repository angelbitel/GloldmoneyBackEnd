namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface ILegacyEmpenioRepository
{
    Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken);
    Task<EmpresaCajaDto?> GetEmpresaByCodigoAsync(string codigoEmpresa, CancellationToken cancellationToken);

    /// <summary>Resta el capital prestado de la caja en la propia sentencia; falla si la caja no alcanza.</summary>
    Task DescontarCajaAsync(string codigoEmpresa, decimal monto, CancellationToken cancellationToken);
    Task<bool> ContratoExistsAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken);
    Task<ClienteDbDto?> GetClienteByIdAsync(string idCliente, CancellationToken cancellationToken);
    void AddCliente(ClienteDbUpsertDto dto);
    void AddContrato(CrearEmpenioContratoDto dto);
    void AddDetalle(CrearEmpenioDetalleDto detalle, string codigoEmpresa, int codigoGrupo, string numeroContrato, int secuencia, int codigoCategoriaPrenda, int? controlReloj);

    /// <summary>Movimiento de caja del empeño (MOVIMIENTO_CAJA y MOVIMIENTO_TEMPORAL), numerado con SEC_CAJA.</summary>
    Task AgregarMovimientoCajaAsync(CrearEmpenioContratoDto dto, string codigoTransaccion, CancellationToken cancellationToken);
    Task<bool> CategoriaPrendaExistsAsync(int codigoCategoriaPrenda, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
