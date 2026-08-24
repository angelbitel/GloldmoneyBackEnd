using GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;

namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IEmpeniosReadService
{
    Task<ContratoDto?> GetContratoByIdAsync(string contratoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContratoDto>> GetContratosByCedulaAsync(string cedula, CancellationToken cancellationToken);
}
