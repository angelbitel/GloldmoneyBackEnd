using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Cuentas;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class CuentasUsuarioService : ICuentasUsuarioService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly LegacyDataDbContext _legacyDbContext;

    public CuentasUsuarioService(ApplicationDbContext dbContext, LegacyDataDbContext legacyDbContext)
    {
        _dbContext = dbContext;
        _legacyDbContext = legacyDbContext;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetCorreosAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.UsuariosCorreo
            .AsNoTracking()
            .ToDictionaryAsync(x => x.NombreUsuario.Trim(), x => x.Correo, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    public Task<bool> UsuarioExisteAsync(string nombreUsuario, CancellationToken cancellationToken)
    {
        var nombre = nombreUsuario.Trim();
        return _legacyDbContext.Usuarios.AsNoTracking().AnyAsync(x => x.NombreUsuario == nombre, cancellationToken);
    }

    public async Task GuardarCorreoAsync(string nombreUsuario, string? correo, CancellationToken cancellationToken)
    {
        var nombre = nombreUsuario.Trim();

        if (correo is null)
        {
            await _dbContext.UsuariosCorreo.Where(x => x.NombreUsuario == nombre).ExecuteDeleteAsync(cancellationToken);
            return;
        }

        var usadoPorOtro = await _dbContext.UsuariosCorreo
            .AnyAsync(x => x.Correo == correo && x.NombreUsuario != nombre, cancellationToken);
        if (usadoPorOtro)
        {
            throw new ConflictDomainException("El correo ya está registrado para otro usuario.");
        }

        var existente = await _dbContext.UsuariosCorreo.FirstOrDefaultAsync(x => x.NombreUsuario == nombre, cancellationToken);
        if (existente is null)
        {
            _dbContext.UsuariosCorreo.Add(new UsuarioCorreo { NombreUsuario = nombre, Correo = correo, ActualizadoEnUtc = DateTime.UtcNow });
        }
        else
        {
            existente.Correo = correo;
            existente.ActualizadoEnUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UsuarioCorreoDto?> BuscarPorUsuarioOCorreoAsync(string usuarioOCorreo, CancellationToken cancellationToken)
    {
        var valor = usuarioOCorreo.Trim();
        var porCorreo = valor.Contains('@');
        var correo = valor.ToLowerInvariant();

        var registro = await _dbContext.UsuariosCorreo
            .AsNoTracking()
            .Where(x => porCorreo ? x.Correo == correo : x.NombreUsuario == valor)
            .Select(x => new UsuarioCorreoDto(x.NombreUsuario, x.Correo))
            .FirstOrDefaultAsync(cancellationToken);

        // Si el usuario se eliminó en el VB6, su correo ya no sirve para recuperar acceso.
        if (registro is null || !await UsuarioExisteAsync(registro.NombreUsuario, cancellationToken))
        {
            return null;
        }

        return registro with { NombreUsuario = registro.NombreUsuario.Trim() };
    }

    public Task<int> ContarSolicitudesDesdeAsync(string nombreUsuario, DateTime desdeUtc, CancellationToken cancellationToken)
    {
        var nombre = nombreUsuario.Trim();
        return _dbContext.RestablecimientosContrasena
            .CountAsync(x => x.NombreUsuario == nombre && x.CreadoEnUtc >= desdeUtc, cancellationToken);
    }

    public async Task RegistrarSolicitudAsync(
        string nombreUsuario,
        string tokenHash,
        DateTime creadoEnUtc,
        DateTime expiraEnUtc,
        CancellationToken cancellationToken)
    {
        var nombre = nombreUsuario.Trim();

        // Solo el enlace más reciente es válido.
        await _dbContext.RestablecimientosContrasena
            .Where(x => x.NombreUsuario == nombre && x.UsadoEnUtc == null && x.ExpiraEnUtc > creadoEnUtc)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiraEnUtc, creadoEnUtc), cancellationToken);

        _dbContext.RestablecimientosContrasena.Add(new RestablecimientoContrasena
        {
            Id = Guid.NewGuid(),
            NombreUsuario = nombre,
            TokenHash = tokenHash,
            CreadoEnUtc = creadoEnUtc,
            ExpiraEnUtc = expiraEnUtc
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RestablecerContrasenaAsync(string tokenHash, string nuevaContrasena, DateTime ahoraUtc, CancellationToken cancellationToken)
    {
        await using var transaccion = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var solicitud = await _dbContext.RestablecimientosContrasena
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.UsadoEnUtc == null && x.ExpiraEnUtc > ahoraUtc, cancellationToken);
        if (solicitud is null)
        {
            return false;
        }

        // La condición UsadoEnUtc == null evita que dos peticiones simultáneas usen el mismo enlace.
        var marcadas = await _dbContext.RestablecimientosContrasena
            .Where(x => x.Id == solicitud.Id && x.UsadoEnUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsadoEnUtc, ahoraUtc), cancellationToken);
        if (marcadas != 1)
        {
            return false;
        }

        // Se guarda en el mismo formato que usa el VB6 (texto, máx. 25), en todos los módulos
        // del usuario, para que la contraseña nueva sirva en ambos sistemas.
        var filas = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE USUARIOS SET contrasena = {nuevaContrasena} WHERE nombre_usuario = {solicitud.NombreUsuario}",
            cancellationToken);
        if (filas == 0)
        {
            await transaccion.RollbackAsync(cancellationToken);
            return false;
        }

        await transaccion.CommitAsync(cancellationToken);
        return true;
    }
}
