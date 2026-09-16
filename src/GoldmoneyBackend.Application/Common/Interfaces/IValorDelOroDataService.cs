namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IValorDelOroDataService
{
    Task<IReadOnlyList<ValorDelOroDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ValorDelOroDbDto?> GetByKeyAsync(
        string codigoEmpresa,
        string statusCalidad,
        decimal kilataje,
        CancellationToken cancellationToken);
    Task CreateAsync(ValorDelOroDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(
        string codigoEmpresa,
        string statusCalidad,
        decimal kilataje,
        ValorDelOroDbUpsertDto dto,
        CancellationToken cancellationToken);
    Task DeleteAsync(
        string codigoEmpresa,
        string statusCalidad,
        decimal kilataje,
        CancellationToken cancellationToken);
}
