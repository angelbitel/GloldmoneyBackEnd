namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface ILegacyEmpenioRepository
{
    Task<EmpresaCajaDto?> GetEmpresaByCodigoAsync(string codigoEmpresa, CancellationToken cancellationToken);
    Task UpdateMontoAuxiliarAsync(string codigoEmpresa, decimal nuevoMonto, CancellationToken cancellationToken);
    Task<bool> ContratoExistsAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken);
    Task<ClienteDbDto?> GetClienteByIdAsync(string idCliente, CancellationToken cancellationToken);
    void AddCliente(ClienteDbUpsertDto dto);
    void AddContrato(CrearEmpenioContratoDto dto);
    Task<int> GetUltimaSecuenciaDetalleAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken);
    void AddDetalle(CrearEmpenioDetalleDto detalle, string codigoEmpresa, int codigoGrupo, string numeroContrato, int secuencia, int codigoCategoriaPrenda, int? controlReloj);
    Task<int> GetUltimoNumeroMovimientoAsync(string codigoEmpresa, CancellationToken cancellationToken);
    void AddMovimientoCaja(CrearEmpenioContratoDto dto, int numeroMovimiento, string codigoTransaccion, DateTime horaTransaccion);
    void AddMovimientoTemporal(CrearEmpenioContratoDto dto, int numeroMovimiento, string codigoTransaccion, DateTime horaTransaccion);
    Task<bool> CategoriaPrendaExistsAsync(int codigoCategoriaPrenda, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}