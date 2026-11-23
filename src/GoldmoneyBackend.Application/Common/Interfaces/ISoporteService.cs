using GoldmoneyBackend.Application.Soporte;

namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface ISoporteService
{
    Task<bool> EsSoporteAsync(UsuarioSoporte usuario, CancellationToken cancellationToken);
    Task<TicketsPaginaDto> GetTicketsAsync(UsuarioSoporte usuario, FiltroTickets filtro, bool bandeja, CancellationToken cancellationToken);
    Task<TicketDetalleDto> GetTicketAsync(UsuarioSoporte usuario, int idTicket, CancellationToken cancellationToken);
    Task<TicketDetalleDto> CrearAsync(UsuarioSoporte usuario, CrearTicketDto dto, CancellationToken cancellationToken);
    Task<TicketDetalleDto> ResponderAsync(UsuarioSoporte usuario, int idTicket, ResponderTicketDto dto, CancellationToken cancellationToken);
    Task<TicketDetalleDto> CerrarAsync(UsuarioSoporte usuario, int idTicket, CancellationToken cancellationToken);
    Task<TicketDetalleDto> ActualizarAsync(UsuarioSoporte usuario, int idTicket, ActualizarTicketDto dto, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgenteSoporteDto>> GetAgentesAsync(CancellationToken cancellationToken);
    Task<TicketDetalleDto> SubirAdjuntosAsync(UsuarioSoporte usuario, int idTicket, int idMensaje, IReadOnlyList<AdjuntoEntrante> archivos, CancellationToken cancellationToken);
    Task<AdjuntoArchivoDto> GetAdjuntoAsync(UsuarioSoporte usuario, int idTicket, int idAdjunto, CancellationToken cancellationToken);
}
