namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IContratoNumeracionRepository
{
    Task<ProximoContratoDto> GetProximoAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken);
}