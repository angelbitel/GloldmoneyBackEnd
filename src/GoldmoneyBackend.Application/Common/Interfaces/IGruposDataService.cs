namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IGruposDataService
{
    Task<IReadOnlyList<GrupoDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<GrupoDbDto?> GetByKeyAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken);
    Task CreateAsync(GrupoDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(string codigoEmpresa, int codigoGrupo, GrupoDbUpsertDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken);
}
