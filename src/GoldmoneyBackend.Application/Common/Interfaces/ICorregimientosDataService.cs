namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface ICorregimientosRepository
{
    Task<IReadOnlyList<CorregimientoDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CorregimientoDbDto?> GetByKeyAsync(string codigoCorregimiento, CancellationToken cancellationToken);
    Task CreateAsync(CorregimientoDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(string codigoCorregimiento, CorregimientoDbUpsertDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(string codigoCorregimiento, CancellationToken cancellationToken);
}
