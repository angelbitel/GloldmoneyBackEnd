using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Soporte;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Soporte;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

/// <summary>
/// Casos de soporte. Todos los usuarios abren y siguen casos; el equipo de soporte (rol Admin o
/// usuario_soporte / usuario_admin en USUARIOS) ve la bandeja, cambia estado, prioridad y
/// asignación y escribe notas internas que el resto no ve.
/// </summary>
public sealed class SoporteService : ISoporteService
{
    private const string Cliente = "Cliente";
    private const string Soporte = "Soporte";

    private readonly ApplicationDbContext _db;
    private readonly LegacyDataDbContext _legacy;

    public SoporteService(ApplicationDbContext db, LegacyDataDbContext legacy)
    {
        _db = db;
        _legacy = legacy;
    }

    public async Task<bool> EsSoporteAsync(UsuarioSoporte usuario, CancellationToken cancellationToken)
    {
        if (usuario.EsAdmin)
        {
            return true;
        }

        var nombre = usuario.Usuario.Trim();
        return await _legacy.Usuarios
            .AsNoTracking()
            .AnyAsync(x => x.NombreUsuario == nombre
                && (x.UsuarioSoporte == 1 || x.UsuarioAdmin == 1)
                && (x.StatusCuenta ?? 0) == 0, cancellationToken);
    }

    public async Task<TicketsPaginaDto> GetTicketsAsync(UsuarioSoporte usuario, FiltroTickets filtro, bool bandeja, CancellationToken cancellationToken)
    {
        if (bandeja && !await EsSoporteAsync(usuario, cancellationToken))
        {
            throw new System.Security.SecurityException();
        }

        if (!bandeja && string.IsNullOrWhiteSpace(filtro.CodigoEmpresa))
        {
            throw new DomainValidationException("Indica la empresa.");
        }

        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize <= 0 ? 50 : filtro.PageSize, 1, 200);

        var query = _db.Tickets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.CodigoEmpresa))
        {
            var empresa = filtro.CodigoEmpresa.Trim();
            query = query.Where(x => x.CodigoEmpresa == empresa);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Categoria))
        {
            query = query.Where(x => x.Categoria == filtro.Categoria);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Prioridad))
        {
            query = query.Where(x => x.Prioridad == filtro.Prioridad);
        }

        if (filtro.SoloMios)
        {
            var yo = usuario.Usuario.Trim();
            query = query.Where(x => x.UsuarioAsignado == yo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            var digitos = new string(texto.Where(char.IsDigit).ToArray());
            var id = digitos.Length is > 0 and <= 9 ? int.Parse(digitos) : -1;
            query = query.Where(x =>
                x.Id == id
                || x.Asunto.Contains(texto)
                || x.CreadoPor.Contains(texto)
                || x.Usuario.Contains(texto)
                || (x.NumeroContrato != null && x.NumeroContrato.Contains(texto)));
        }

        // Los conteos por estado ignoran el filtro de estado: alimentan los botones de la bandeja.
        var porEstado = await query
            .GroupBy(x => x.Estado)
            .Select(g => new { Estado = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Estado, x => x.Total, cancellationToken);

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            query = query.Where(x => x.Estado == filtro.Estado);
        }
        else if (!filtro.IncluirCerrados)
        {
            query = query.Where(x => x.Estado != "Cerrado");
        }

        var total = await query.CountAsync(cancellationToken);
        var tickets = await query
            .OrderByDescending(x => x.FechaActualizacionUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var empresas = await GetNombresEmpresaAsync(cancellationToken);
        var items = tickets.Select(t => ToResumen(t, empresas)).ToList();

        return new TicketsPaginaDto(total, page, pageSize, items, porEstado);
    }

    public async Task<TicketDetalleDto> GetTicketAsync(UsuarioSoporte usuario, int idTicket, CancellationToken cancellationToken)
    {
        var esSoporte = await EsSoporteAsync(usuario, cancellationToken);
        return await GetDetalleAsync(idTicket, esSoporte, null, cancellationToken);
    }

    public async Task<TicketDetalleDto> CrearAsync(UsuarioSoporte usuario, CrearTicketDto dto, CancellationToken cancellationToken)
    {
        var empresa = dto.CodigoEmpresa?.Trim() ?? string.Empty;
        var asunto = dto.Asunto?.Trim() ?? string.Empty;
        var mensaje = dto.Mensaje?.Trim() ?? string.Empty;

        if (empresa.Length == 0 || !await _legacy.Empresas.AnyAsync(x => x.CodigoEmpresa == empresa, cancellationToken))
        {
            throw new DomainValidationException("La empresa no existe.");
        }

        if (asunto.Length < 3 || asunto.Length > 200)
        {
            throw new DomainValidationException("Escribe un asunto (de 3 a 200 caracteres).");
        }

        if (!SoporteCatalogo.Categorias.Contains(dto.Categoria))
        {
            throw new DomainValidationException("Categoría no válida.");
        }

        if (!SoporteCatalogo.Prioridades.Contains(dto.Prioridad))
        {
            throw new DomainValidationException("Prioridad no válida.");
        }

        ValidarMensaje(mensaje, 5);

        var esSoporte = await EsSoporteAsync(usuario, cancellationToken);
        var (nombreUsuario, autor) = await GetAutorAsync(usuario, cancellationToken);
        var ahora = DateTime.UtcNow;

        var ticket = new Ticket
        {
            CodigoEmpresa = empresa,
            Asunto = asunto,
            Categoria = dto.Categoria,
            Prioridad = dto.Prioridad,
            Estado = "Abierto",
            Origen = string.IsNullOrWhiteSpace(dto.Origen) ? "Web" : dto.Origen.Trim()[..Math.Min(dto.Origen.Trim().Length, 20)],
            Usuario = nombreUsuario,
            CreadoPor = autor,
            UltimaRespuestaDe = Cliente,
            NumeroContrato = Recortar(dto.NumeroContrato, 30),
            Pantalla = Recortar(dto.Pantalla, 100),
            FechaCreacionUtc = ahora,
            FechaActualizacionUtc = ahora
        };

        var primero = new TicketMensaje
        {
            Usuario = nombreUsuario,
            Autor = autor,
            EsSoporte = false,
            Mensaje = mensaje,
            FechaUtc = ahora
        };
        ticket.Mensajes.Add(primero);

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetDetalleAsync(ticket.Id, esSoporte, primero.Id, cancellationToken);
    }

    public async Task<TicketDetalleDto> ResponderAsync(UsuarioSoporte usuario, int idTicket, ResponderTicketDto dto, CancellationToken cancellationToken)
    {
        var mensaje = dto.Mensaje?.Trim() ?? string.Empty;
        ValidarMensaje(mensaje, 1);

        var esSoporte = await EsSoporteAsync(usuario, cancellationToken);
        var ticket = await FindTicketAsync(idTicket, cancellationToken);

        if (dto.EsInterno && !esSoporte)
        {
            throw new System.Security.SecurityException();
        }

        if (!esSoporte && ticket.Estado == "Cerrado")
        {
            throw new ConflictDomainException("El caso está cerrado. Si el problema sigue, abre un caso nuevo.");
        }

        if (dto.NuevoEstado is not null && (!esSoporte || !SoporteCatalogo.Estados.Contains(dto.NuevoEstado)))
        {
            throw new DomainValidationException("Estado no válido.");
        }

        var (nombreUsuario, autor) = await GetAutorAsync(usuario, cancellationToken);
        var ahora = DateTime.UtcNow;

        var nuevo = new TicketMensaje
        {
            TicketId = ticket.Id,
            Usuario = nombreUsuario,
            Autor = autor,
            EsSoporte = esSoporte,
            EsInterno = dto.EsInterno,
            Mensaje = mensaje,
            FechaUtc = ahora
        };
        _db.TicketMensajes.Add(nuevo);

        ticket.FechaActualizacionUtc = ahora;

        if (!dto.EsInterno)
        {
            ticket.UltimaRespuestaDe = esSoporte ? Soporte : Cliente;

            if (esSoporte)
            {
                CambiarEstado(ticket, dto.NuevoEstado ?? "EsperandoCliente", ahora);

                // Quien responde primero se queda con el caso.
                if (ticket.UsuarioAsignado is null)
                {
                    ticket.UsuarioAsignado = nombreUsuario;
                    ticket.AsignadoA = autor;
                }
            }
            else if (ticket.Estado is "EsperandoCliente" or "Resuelto")
            {
                // El cliente volvió a escribir: el caso vuelve a la cola de soporte.
                CambiarEstado(ticket, "Abierto", ahora);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetDetalleAsync(ticket.Id, esSoporte, nuevo.Id, cancellationToken);
    }

    public async Task<TicketDetalleDto> CerrarAsync(UsuarioSoporte usuario, int idTicket, CancellationToken cancellationToken)
    {
        var esSoporte = await EsSoporteAsync(usuario, cancellationToken);
        var ticket = await FindTicketAsync(idTicket, cancellationToken);

        if (ticket.Estado != "Cerrado")
        {
            var ahora = DateTime.UtcNow;
            CambiarEstado(ticket, "Cerrado", ahora);
            ticket.FechaActualizacionUtc = ahora;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await GetDetalleAsync(ticket.Id, esSoporte, null, cancellationToken);
    }

    public async Task<TicketDetalleDto> ActualizarAsync(UsuarioSoporte usuario, int idTicket, ActualizarTicketDto dto, CancellationToken cancellationToken)
    {
        if (!await EsSoporteAsync(usuario, cancellationToken))
        {
            throw new System.Security.SecurityException();
        }

        var ticket = await FindTicketAsync(idTicket, cancellationToken);
        var ahora = DateTime.UtcNow;

        if (dto.Estado is not null)
        {
            if (!SoporteCatalogo.Estados.Contains(dto.Estado))
            {
                throw new DomainValidationException("Estado no válido.");
            }

            CambiarEstado(ticket, dto.Estado, ahora);
        }

        if (dto.Prioridad is not null)
        {
            if (!SoporteCatalogo.Prioridades.Contains(dto.Prioridad))
            {
                throw new DomainValidationException("Prioridad no válida.");
            }

            ticket.Prioridad = dto.Prioridad;
        }

        if (dto.UsuarioAsignado is not null)
        {
            if (dto.UsuarioAsignado.Trim().Length == 0)
            {
                ticket.UsuarioAsignado = null;
                ticket.AsignadoA = null;
            }
            else
            {
                var agente = (await GetAgentesAsync(cancellationToken))
                    .FirstOrDefault(a => string.Equals(a.Usuario, dto.UsuarioAsignado.Trim(), StringComparison.OrdinalIgnoreCase))
                    ?? throw new DomainValidationException("Ese usuario no es del equipo de soporte.");
                ticket.UsuarioAsignado = agente.Usuario;
                ticket.AsignadoA = agente.Nombre;
            }
        }

        ticket.FechaActualizacionUtc = ahora;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetDetalleAsync(ticket.Id, true, null, cancellationToken);
    }

    public async Task<IReadOnlyList<AgenteSoporteDto>> GetAgentesAsync(CancellationToken cancellationToken)
    {
        var usuarios = await _legacy.Usuarios
            .AsNoTracking()
            .Where(x => (x.UsuarioSoporte == 1 || x.UsuarioAdmin == 1) && (x.StatusCuenta ?? 0) == 0)
            .Select(x => new { x.NombreUsuario, x.NombreCompleto })
            .ToListAsync(cancellationToken);

        return usuarios
            .GroupBy(x => x.NombreUsuario.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(x => new AgenteSoporteDto(
                x.NombreUsuario.Trim(),
                string.IsNullOrWhiteSpace(x.NombreCompleto) ? x.NombreUsuario.Trim() : x.NombreCompleto.Trim()))
            .OrderBy(x => x.Nombre)
            .ToList();
    }

    public async Task<TicketDetalleDto> SubirAdjuntosAsync(
        UsuarioSoporte usuario,
        int idTicket,
        int idMensaje,
        IReadOnlyList<AdjuntoEntrante> archivos,
        CancellationToken cancellationToken)
    {
        if (archivos.Count == 0)
        {
            throw new DomainValidationException("No llegó ningún archivo.");
        }

        var mensaje = await _db.TicketMensajes
            .Include(x => x.Adjuntos)
            .FirstOrDefaultAsync(x => x.Id == idMensaje && x.TicketId == idTicket, cancellationToken)
            ?? throw new NotFoundDomainException("No existe el mensaje.");

        // Solo el autor sube archivos a su propio mensaje.
        if (!string.Equals(mensaje.Usuario, usuario.Usuario.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new System.Security.SecurityException();
        }

        if (mensaje.Adjuntos.Count + archivos.Count > SoporteCatalogo.AdjuntosMaxPorMensaje)
        {
            throw new DomainValidationException($"Puedes adjuntar hasta {SoporteCatalogo.AdjuntosMaxPorMensaje} archivos por mensaje.");
        }

        var ahora = DateTime.UtcNow;
        foreach (var archivo in archivos)
        {
            var nombre = Path.GetFileName(archivo.Nombre ?? string.Empty).Trim();
            var extension = Path.GetExtension(nombre).TrimStart('.').ToLowerInvariant();

            if (nombre.Length == 0 || !SoporteCatalogo.AdjuntoExtensiones.Contains(extension))
            {
                throw new DomainValidationException($"\"{nombre}\": tipo de archivo no permitido.");
            }

            if (archivo.Tamano <= 0)
            {
                throw new DomainValidationException($"\"{nombre}\" está vacío.");
            }

            if (archivo.Tamano > SoporteCatalogo.AdjuntoMaxBytes)
            {
                throw new DomainValidationException($"\"{nombre}\" pesa más de 10 MB.");
            }

            using var memoria = new MemoryStream();
            await using (var origen = archivo.Abrir())
            {
                await origen.CopyToAsync(memoria, cancellationToken);
            }

            mensaje.Adjuntos.Add(new TicketAdjunto
            {
                Nombre = nombre.Length > 255 ? nombre[^255..] : nombre,
                ContentType = ContentTypeDe(extension),
                Tamano = memoria.Length,
                Contenido = memoria.ToArray(),
                FechaUtc = ahora
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var esSoporte = await EsSoporteAsync(usuario, cancellationToken);
        return await GetDetalleAsync(idTicket, esSoporte, null, cancellationToken);
    }

    public async Task<AdjuntoArchivoDto> GetAdjuntoAsync(UsuarioSoporte usuario, int idTicket, int idAdjunto, CancellationToken cancellationToken)
    {
        var datos = await (
                from a in _db.TicketAdjuntos.AsNoTracking()
                join m in _db.TicketMensajes.AsNoTracking() on a.MensajeId equals m.Id
                where a.Id == idAdjunto && m.TicketId == idTicket
                select new { a.Nombre, a.ContentType, a.Contenido, m.EsInterno })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundDomainException("No existe el archivo.");

        if (datos.EsInterno && !await EsSoporteAsync(usuario, cancellationToken))
        {
            throw new System.Security.SecurityException();
        }

        return new AdjuntoArchivoDto(datos.Nombre, datos.ContentType, datos.Contenido);
    }

    // ---------- Auxiliares ----------

    private async Task<Ticket> FindTicketAsync(int idTicket, CancellationToken cancellationToken) =>
        await _db.Tickets.FirstOrDefaultAsync(x => x.Id == idTicket, cancellationToken)
        ?? throw new NotFoundDomainException("No existe el caso.");

    private async Task<TicketDetalleDto> GetDetalleAsync(int idTicket, bool esSoporte, int? idMensajeNuevo, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idTicket, cancellationToken)
            ?? throw new NotFoundDomainException("No existe el caso.");

        var mensajes = await _db.TicketMensajes
            .AsNoTracking()
            .Where(x => x.TicketId == idTicket && (esSoporte || !x.EsInterno))
            .OrderBy(x => x.FechaUtc)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Usuario,
                x.Autor,
                x.EsSoporte,
                x.EsInterno,
                x.Mensaje,
                x.FechaUtc,
                // Sin el contenido: solo los datos para listar.
                Adjuntos = x.Adjuntos
                    .OrderBy(a => a.Id)
                    .Select(a => new { a.Id, a.Nombre, a.ContentType, a.Tamano, a.FechaUtc })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        string? correo = null;
        if (esSoporte)
        {
            correo = await _db.UsuariosCorreo
                .AsNoTracking()
                .Where(x => x.NombreUsuario == ticket.Usuario)
                .Select(x => x.Correo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var empresas = await GetNombresEmpresaAsync(cancellationToken);

        return new TicketDetalleDto(ToResumen(ticket, empresas))
        {
            NumeroContrato = ticket.NumeroContrato,
            Pantalla = ticket.Pantalla,
            EmailUsuario = correo,
            VistaSoporte = esSoporte,
            IdMensajeNuevo = idMensajeNuevo,
            Mensajes = mensajes.Select(m => new TicketMensajeDto(
                    m.Id,
                    m.Usuario,
                    m.Autor,
                    m.EsSoporte,
                    m.EsInterno,
                    m.Mensaje,
                    m.FechaUtc,
                    m.Adjuntos
                        .Select(a => new TicketAdjuntoDto(a.Id, a.Nombre, a.ContentType, a.Tamano, a.ContentType.StartsWith("image/"), a.FechaUtc))
                        .ToList()))
                .ToList()
        };
    }

    private async Task<Dictionary<string, string>> GetNombresEmpresaAsync(CancellationToken cancellationToken)
    {
        var empresas = await _legacy.Empresas
            .AsNoTracking()
            .Select(x => new { x.CodigoEmpresa, x.NombreEmpresa })
            .ToListAsync(cancellationToken);

        return empresas
            .GroupBy(x => x.CodigoEmpresa.Trim())
            .ToDictionary(g => g.Key, g => g.First().NombreEmpresa?.Trim() ?? g.Key);
    }

    private async Task<(string Usuario, string Autor)> GetAutorAsync(UsuarioSoporte usuario, CancellationToken cancellationToken)
    {
        var nombre = usuario.Usuario.Trim();
        var completo = await _legacy.Usuarios
            .AsNoTracking()
            .Where(x => x.NombreUsuario == nombre)
            .Select(x => x.NombreCompleto)
            .FirstOrDefaultAsync(cancellationToken);

        var autor = string.IsNullOrWhiteSpace(completo) ? nombre : completo.Trim();
        return (nombre, autor.Length > 100 ? autor[..100] : autor);
    }

    private static TicketResumenDto ToResumen(Ticket t, IReadOnlyDictionary<string, string> empresas) => new(
        t.Id,
        Numero(t.Id),
        t.CodigoEmpresa,
        empresas.GetValueOrDefault(t.CodigoEmpresa.Trim(), t.CodigoEmpresa),
        t.Asunto,
        t.Categoria,
        t.Prioridad,
        t.Estado,
        t.Origen,
        t.Usuario,
        t.CreadoPor,
        t.UsuarioAsignado,
        t.AsignadoA,
        t.UltimaRespuestaDe,
        DateTime.SpecifyKind(t.FechaCreacionUtc, DateTimeKind.Utc),
        DateTime.SpecifyKind(t.FechaActualizacionUtc, DateTimeKind.Utc),
        t.FechaCierreUtc is { } cierre ? DateTime.SpecifyKind(cierre, DateTimeKind.Utc) : null);

    private static string Numero(int id) => $"GM-{id:D5}";

    private static void CambiarEstado(Ticket ticket, string estado, DateTime ahora)
    {
        ticket.Estado = estado;
        ticket.FechaCierreUtc = estado == "Cerrado" ? ticket.FechaCierreUtc ?? ahora : null;
    }

    private static void ValidarMensaje(string mensaje, int minimo)
    {
        if (mensaje.Length < minimo)
        {
            throw new DomainValidationException("Escribe el mensaje.");
        }

        if (mensaje.Length > 8000)
        {
            throw new DomainValidationException("El mensaje no puede pasar de 8000 caracteres.");
        }
    }

    private static string? Recortar(string? valor, int max)
    {
        var v = valor?.Trim();
        return string.IsNullOrEmpty(v) ? null : v.Length > max ? v[..max] : v;
    }

    private static string ContentTypeDe(string extension) => extension switch
    {
        "jpg" or "jpeg" => "image/jpeg",
        "png" => "image/png",
        "gif" => "image/gif",
        "webp" => "image/webp",
        "pdf" => "application/pdf",
        "txt" or "log" => "text/plain",
        "csv" => "text/csv",
        "xml" => "application/xml",
        "json" => "application/json",
        "xls" => "application/vnd.ms-excel",
        "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "doc" => "application/msword",
        "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "zip" => "application/zip",
        "mp4" => "video/mp4",
        _ => "application/octet-stream"
    };
}
