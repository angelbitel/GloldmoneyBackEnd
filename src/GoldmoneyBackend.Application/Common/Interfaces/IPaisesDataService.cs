namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IPaisesRepository
{
    Task<IReadOnlyList<PaisDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<PaisDbDto?> GetByKeyAsync(string codigoPais, CancellationToken cancellationToken);
    Task CreateAsync(PaisDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(string codigoPais, PaisDbUpsertDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(string codigoPais, CancellationToken cancellationToken);
}
