namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IEmpresasDataService
{
    Task CreateAsync(EmpresaDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(EmpresaDbUpsertDto dto, CancellationToken cancellationToken);
}
