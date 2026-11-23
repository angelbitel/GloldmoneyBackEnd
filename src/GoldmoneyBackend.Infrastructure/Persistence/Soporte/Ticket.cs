namespace GoldmoneyBackend.Infrastructure.Persistence.Soporte;

/// <summary>
/// Caso de soporte: un usuario lo abre desde una empresa y el equipo de soporte
/// (usuarios admin o con usuario_soporte en USUARIOS) lo atiende.
/// </summary>
public sealed class Ticket
{
    public int Id { get; set; }
    public string CodigoEmpresa { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string CreadoPor { get; set; } = string.Empty;
    public string? UsuarioAsignado { get; set; }
    public string? AsignadoA { get; set; }
    /// <summary>"Cliente" o "Soporte": quién escribió el último mensaje visible.</summary>
    public string UltimaRespuestaDe { get; set; } = string.Empty;
    public string? NumeroContrato { get; set; }
    public string? Pantalla { get; set; }
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime FechaActualizacionUtc { get; set; }
    public DateTime? FechaCierreUtc { get; set; }
    public List<TicketMensaje> Mensajes { get; set; } = [];
}

public sealed class TicketMensaje
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public bool EsSoporte { get; set; }
    /// <summary>Nota interna: solo la ve el equipo de soporte.</summary>
    public bool EsInterno { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
    public List<TicketAdjunto> Adjuntos { get; set; } = [];
}

/// <summary>Archivo de un mensaje. Se guarda en la base: en Render el disco no persiste.</summary>
public sealed class TicketAdjunto
{
    public int Id { get; set; }
    public int MensajeId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Tamano { get; set; }
    public byte[] Contenido { get; set; } = [];
    public DateTime FechaUtc { get; set; }
}
