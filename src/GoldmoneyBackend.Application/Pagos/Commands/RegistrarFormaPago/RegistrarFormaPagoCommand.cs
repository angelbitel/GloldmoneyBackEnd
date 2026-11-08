using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarFormaPago;

/// <summary>
/// Registra cómo pagó el cliente un cobro ya grabado (abonos y retiros), con los recibos que cubre.
/// Se envía después de registrar los contratos, solo con los recibos que sí se registraron.
/// </summary>
public sealed record RegistrarFormaPagoCommand(
    string IdCliente,
    IReadOnlyList<MetodoPagoCobroDto> Metodos,
    IReadOnlyList<DocumentoCobroDto> Documentos,
    string UsuarioResponsable) : IRequest<CobroDto>;

public static class MetodosPago
{
    public static readonly IReadOnlySet<string> Validos =
        new HashSet<string>(["efectivo", "yappy", "tarjeta", "transferencia"], StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> TiposDocumento =
        new HashSet<string>(["ABONO", "RETIRO"], StringComparer.Ordinal);
}
