namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface ICategoriasPrendaRepository
{
    Task<IReadOnlyList<CategoriaPrendaDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CategoriaPrendaDbDto?> GetByIdAsync(int codigoCategoriaPrenda, CancellationToken cancellationToken);
}
