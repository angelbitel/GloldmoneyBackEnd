namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IEmpeniosDataService
{
    Task<string> CrearContratoAsync(CrearEmpenioContratoDto dto, CancellationToken cancellationToken);
}
