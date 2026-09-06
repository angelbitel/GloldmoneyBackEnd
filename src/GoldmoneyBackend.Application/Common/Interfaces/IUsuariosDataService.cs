namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IUsuariosDataService
{
    Task<IReadOnlyList<UsuarioDbDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<UsuarioDbDto?> GetByKeyAsync(string modulo, string nombreUsuario, CancellationToken cancellationToken);
    Task CreateAsync(UsuarioDbUpsertDto dto, CancellationToken cancellationToken);
    Task UpdateAsync(string modulo, string nombreUsuario, UsuarioDbUpsertDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(string modulo, string nombreUsuario, CancellationToken cancellationToken);
}
