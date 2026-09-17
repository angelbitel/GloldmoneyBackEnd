namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface ISesionesDataService
{
    Task<IReadOnlyList<EstadoSesionEmpresaDto>> GetEstadoSesionesAsync(CancellationToken cancellationToken);
    Task<SesionAbiertaDto> AbrirSesionAsync(AbrirSesionDto dto, CancellationToken cancellationToken);
}
