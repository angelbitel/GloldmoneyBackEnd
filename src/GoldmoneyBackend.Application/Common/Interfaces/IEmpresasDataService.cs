namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IEmpresasRepository
{
    Task CreateAsync(EmpresaDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(EmpresaDbUpsertDto dto, CancellationToken cancellationToken);
}
