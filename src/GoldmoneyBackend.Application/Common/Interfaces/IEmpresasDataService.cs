namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IEmpresasRepository
{
    Task<IReadOnlyList<EmpresaDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<EmpresaDbDto?> GetByIdAsync(string codigoEmpresa, CancellationToken cancellationToken);
    Task CreateAsync(EmpresaDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(EmpresaDbUpsertDto dto, CancellationToken cancellationToken);
}

/// <summary>Empresa de la tabla legacy EMPRESA. MontoAuxiliar es el dinero actual en caja.</summary>
public sealed record EmpresaDbDto(
    string CodigoEmpresa,
    string? NombreEmpresa,
    string? Direccion,
    string? Ruc,
    string? Telefono,
    decimal? MontoInicial,
    decimal? MontoAuxiliar,
    int? ManejoCajaDep,
    int? CodEmpresaCaja);
