namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IGruposDataService
{
    Task<IReadOnlyList<GrupoDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<GrupoDbDto?> GetByKeyAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken);
}
