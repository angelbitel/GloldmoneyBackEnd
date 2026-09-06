using System.Linq.Expressions;
using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class UsuariosDataService : IUsuariosDataService
{
    private readonly LegacyDataDbContext _dbContext;

    public UsuariosDataService(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UsuarioDbDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Usuarios
            .AsNoTracking()
            .OrderBy(x => x.Modulo)
            .ThenBy(x => x.NombreUsuario)
            .Select(MapToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<UsuarioDbDto?> GetByKeyAsync(string modulo, string nombreUsuario, CancellationToken cancellationToken)
    {
        ValidateRequired(modulo, "modulo");
        ValidateRequired(nombreUsuario, "nombre_usuario");

        var moduloTrimmed = modulo.Trim();
        var nombreUsuarioTrimmed = nombreUsuario.Trim();

        return await _dbContext.Usuarios
            .AsNoTracking()
            .Where(x => x.Modulo == moduloTrimmed && x.NombreUsuario == nombreUsuarioTrimmed)
            .Select(MapToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(UsuarioDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateDto(dto);

        var modulo = dto.Modulo.Trim();
        var nombreUsuario = dto.NombreUsuario.Trim();

        var exists = await _dbContext.Usuarios
            .AsNoTracking()
            .AnyAsync(x => x.Modulo == modulo && x.NombreUsuario == nombreUsuario, cancellationToken);

        if (exists)
        {
            throw new ConflictDomainException("Ya existe un usuario con ese modulo y nombre_usuario.");
        }

        var entity = new UsuarioDb
        {
            Modulo = modulo,
            NombreUsuario = nombreUsuario,
            NombreCompleto = TrimOrNull(dto.NombreCompleto),
            Contrasena = dto.Contrasena.Trim(),
            StatusCuenta = dto.StatusCuenta,
            IniciarDia = dto.IniciarDia,
            AplicarDescuento = dto.AplicarDescuento,
            UsuarioAdmin = dto.UsuarioAdmin,
            DatosRetroactivos = dto.DatosRetroactivos,
            EfectuarAnulacion = dto.EfectuarAnulacion,
            AccesoCashDrawer = dto.AccesoCashDrawer,
            UsuarioSoporte = dto.UsuarioSoporte
        };

        await _dbContext.Usuarios.AddAsync(entity, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo crear el usuario en tabla USUARIOS. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task UpdateAsync(string modulo, string nombreUsuario, UsuarioDbUpsertDto dto, CancellationToken cancellationToken)
    {
        ValidateDto(dto);
        ValidateRequired(modulo, "modulo");
        ValidateRequired(nombreUsuario, "nombre_usuario");

        var moduloTrimmed = modulo.Trim();
        var nombreUsuarioTrimmed = nombreUsuario.Trim();

        var entity = await _dbContext.Usuarios
            .FirstOrDefaultAsync(x => x.Modulo == moduloTrimmed && x.NombreUsuario == nombreUsuarioTrimmed, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Usuario no encontrado en tabla USUARIOS.");
        }

        entity.Modulo = dto.Modulo.Trim();
        entity.NombreUsuario = dto.NombreUsuario.Trim();
        entity.NombreCompleto = TrimOrNull(dto.NombreCompleto);
        entity.Contrasena = dto.Contrasena.Trim();
        entity.StatusCuenta = dto.StatusCuenta;
        entity.IniciarDia = dto.IniciarDia;
        entity.AplicarDescuento = dto.AplicarDescuento;
        entity.UsuarioAdmin = dto.UsuarioAdmin;
        entity.DatosRetroactivos = dto.DatosRetroactivos;
        entity.EfectuarAnulacion = dto.EfectuarAnulacion;
        entity.AccesoCashDrawer = dto.AccesoCashDrawer;
        entity.UsuarioSoporte = dto.UsuarioSoporte;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo actualizar el usuario en tabla USUARIOS. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task DeleteAsync(string modulo, string nombreUsuario, CancellationToken cancellationToken)
    {
        ValidateRequired(modulo, "modulo");
        ValidateRequired(nombreUsuario, "nombre_usuario");

        var moduloTrimmed = modulo.Trim();
        var nombreUsuarioTrimmed = nombreUsuario.Trim();

        var entity = await _dbContext.Usuarios
            .FirstOrDefaultAsync(x => x.Modulo == moduloTrimmed && x.NombreUsuario == nombreUsuarioTrimmed, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundDomainException("Usuario no encontrado en tabla USUARIOS.");
        }

        _dbContext.Usuarios.Remove(entity);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new DomainValidationException($"No se pudo eliminar el usuario en tabla USUARIOS. Detalle: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static Expression<Func<UsuarioDb, UsuarioDbDto>> MapToDto()
    {
        return x => new UsuarioDbDto(
            x.Modulo,
            x.NombreUsuario,
            x.NombreCompleto,
            x.StatusCuenta,
            x.IniciarDia,
            x.AplicarDescuento,
            x.UsuarioAdmin,
            x.DatosRetroactivos,
            x.EfectuarAnulacion,
            x.AccesoCashDrawer,
            x.UsuarioSoporte);
    }

    private static void ValidateDto(UsuarioDbUpsertDto dto)
    {
        ValidateRequired(dto.Modulo, "modulo");
        ValidateRequired(dto.NombreUsuario, "nombre_usuario");
        ValidateRequired(dto.Contrasena, "contrasena");

        if (dto.Modulo.Trim().Length > 2)
        {
            throw new DomainValidationException("modulo no puede exceder 2 caracteres.");
        }

        if (dto.NombreUsuario.Trim().Length > 15)
        {
            throw new DomainValidationException("nombre_usuario no puede exceder 15 caracteres.");
        }

        if (dto.Contrasena.Trim().Length > 25)
        {
            throw new DomainValidationException("contrasena no puede exceder 25 caracteres en la tabla USUARIOS.");
        }
    }

    private static void ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} es obligatorio.");
        }
    }

    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
