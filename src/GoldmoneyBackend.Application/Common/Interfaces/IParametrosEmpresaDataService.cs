namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IParametrosEmpresaDataService
{
    Task<IReadOnlyList<ParametrosEmpresaDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ParametrosEmpresaDbDto?> GetByKeyAsync(string codigoEmpresa, CancellationToken cancellationToken);
    Task CreateAsync(ParametrosEmpresaDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(string codigoEmpresa, ParametrosEmpresaDbUpsertDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(string codigoEmpresa, CancellationToken cancellationToken);
}
