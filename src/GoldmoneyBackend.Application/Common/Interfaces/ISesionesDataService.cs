namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface ISesionesRepository
{
    Task<IReadOnlyList<EstadoSesionEmpresaDto>> GetEstadoSesionesAsync(CancellationToken cancellationToken);
    Task<SesionAbiertaDto> AbrirSesionAsync(AbrirSesionDto dto, CancellationToken cancellationToken);
}
