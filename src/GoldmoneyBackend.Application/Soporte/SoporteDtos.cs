namespace GoldmoneyBackend.Application.Soporte;

/// <summary>Valores permitidos y reglas de adjuntos (el front repite las mismas).</summary>
public static class SoporteCatalogo
{
    public static readonly string[] Estados = ["Abierto", "EnProceso", "EsperandoCliente", "Resuelto", "Cerrado"];
    public static readonly string[] Prioridades = ["Baja", "Media", "Alta", "Urgente"];
    public static readonly string[] Categorias =
        ["Empenos", "Cobros", "Caja", "Clientes", "Inventario", "Impresion", "Usuarios", "Otro"];

    public const long AdjuntoMaxBytes = 10 * 1024 * 1024;
    public const int AdjuntosMaxPorMensaje = 5;
    public static readonly string[] AdjuntoExtensiones =
        ["jpg", "jpeg", "png", "gif", "webp", "pdf", "txt", "log", "csv", "xml", "json", "xls", "xlsx", "doc", "docx", "zip", "mp4"];
}

/// <summary>Quién hace la petición. EsSoporte lo resuelve el servicio (rol Admin o USUARIOS).</summary>
public sealed record UsuarioSoporte(string Usuario, bool EsAdmin);

public record TicketResumenDto(
    int IdTicket,
    string Numero,
    string CodigoEmpresa,
    string Empresa,
    string Asunto,
    string Categoria,
    string Prioridad,
    string Estado,
    string Origen,
    string Usuario,
    string CreadoPor,
    string? UsuarioAsignado,
    string? AsignadoA,
    string UltimaRespuestaDe,
    DateTime FechaCreacion,
    DateTime FechaActualizacion,
    DateTime? FechaCierre);

public sealed record TicketAdjuntoDto(int IdAdjunto, string Nombre, string ContentType, long Tamano, bool EsImagen, DateTime Fecha);

public sealed record TicketMensajeDto(
    int IdMensaje,
    string Usuario,
    string Autor,
    bool EsSoporte,
    bool EsInterno,
    string Mensaje,
    DateTime Fecha,
    IReadOnlyList<TicketAdjuntoDto> Adjuntos);

public sealed record TicketDetalleDto : TicketResumenDto
{
    public TicketDetalleDto(TicketResumenDto resumen) : base(resumen) { }

    public string? NumeroContrato { get; init; }
    public string? Pantalla { get; init; }
    public string? EmailUsuario { get; init; }
    public bool VistaSoporte { get; init; }
    public IReadOnlyList<TicketMensajeDto> Mensajes { get; init; } = [];
    /// <summary>Mensaje recién creado (al abrir el caso o responder), para subirle los adjuntos.</summary>
    public int? IdMensajeNuevo { get; init; }
}

public sealed record TicketsPaginaDto(int Total, int Page, int PageSize, IReadOnlyList<TicketResumenDto> Items, IReadOnlyDictionary<string, int> PorEstado);

public sealed record AgenteSoporteDto(string Usuario, string Nombre);

public sealed record FiltroTickets(
    string? CodigoEmpresa,
    string? Estado,
    string? Categoria,
    string? Prioridad,
    string? Texto,
    bool SoloMios,
    bool IncluirCerrados,
    int Page,
    int PageSize);

public sealed record CrearTicketDto(
    string CodigoEmpresa,
    string Asunto,
    string Categoria,
    string Prioridad,
    string Mensaje,
    string? NumeroContrato,
    string? Pantalla,
    string? Origen);

public sealed record ResponderTicketDto(string Mensaje, bool EsInterno, string? NuevoEstado);

/// <summary>UsuarioAsignado: "" quita la asignación; null no la cambia.</summary>
public sealed record ActualizarTicketDto(string? Estado, string? Prioridad, string? UsuarioAsignado);

public sealed record AdjuntoEntrante(string Nombre, string ContentType, long Tamano, Func<Stream> Abrir);

public sealed record AdjuntoArchivoDto(string Nombre, string ContentType, byte[] Contenido);
