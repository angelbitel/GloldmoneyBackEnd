using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy;
using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class LegacyEmpenioRepository : ILegacyEmpenioRepository
{
    private readonly LegacyDataDbContext _dbContext;

    public LegacyEmpenioRepository(LegacyDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmpresaCajaDto?> GetEmpresaByCodigoAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa.Trim(), cancellationToken);

        if (entity is null) return null;

        return new EmpresaCajaDto(
            entity.CodigoEmpresa,
            entity.MontoInicial,
            entity.MontoAuxiliar,
            entity.ManejoCajaDep);
    }

    public async Task UpdateMontoAuxiliarAsync(string codigoEmpresa, decimal nuevoMonto, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Empresas
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa.Trim(), cancellationToken);

        if (entity is not null)
        {
            entity.MontoAuxiliar = nuevoMonto;
        }
    }

    public Task<bool> ContratoExistsAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken)
    {
        return _dbContext.Contratos
            .AsNoTracking()
            .AnyAsync(x => x.CodigoEmpresa == codigoEmpresa
                && x.CodigoGrupo == codigoGrupo
                && x.NumeroContrato == numeroContrato, cancellationToken);
    }

    public async Task<ClienteDbDto?> GetClienteByIdAsync(string idCliente, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Clientes
            .FirstOrDefaultAsync(x => x.IdCliente == idCliente, cancellationToken);

        if (entity is null) return null;

        return new ClienteDbDto(
            entity.IdCliente,
            entity.Apellido,
            entity.Nombre,
            entity.Telefono,
            entity.Estatus,
            entity.Direccion,
            entity.Comentario,
            entity.CodigoPais,
            entity.CodigoProvincia,
            entity.CodigoDistrito,
            entity.CodigoCorregimiento);
    }

    public void AddCliente(ClienteDbUpsertDto dto)
    {
        _dbContext.Clientes.Add(new ClienteDb
        {
            IdCliente = dto.IdCliente,
            Apellido = dto.Apellido,
            Nombre = dto.Nombre,
            Telefono = dto.Telefono,
            Estatus = dto.Estatus ?? 1,
            Direccion = dto.Direccion,
            Comentario = dto.Comentario,
            CodigoPais = dto.CodigoPais,
            CodigoProvincia = dto.CodigoProvincia,
            CodigoDistrito = dto.CodigoDistrito,
            CodigoCorregimiento = dto.CodigoCorregimiento
        });
    }

    public void AddContrato(CrearEmpenioContratoDto dto)
    {
        _dbContext.Contratos.Add(new ContratoDb
        {
            CodigoEmpresa = dto.CodigoEmpresa.Trim(),
            CodigoGrupo = dto.CodigoGrupo,
            NumeroContrato = dto.NumeroContrato.Trim(),
            IdCliente = dto.IdCliente,
            Serie = dto.Serie.Trim(),
            FechaCreacion = dto.FechaCreacion,
            CapitalPrestado = dto.CapitalPrestado,
            Interes = dto.Interes,
            SaldoActual = dto.SaldoActual,
            InteresMensual = dto.Mensualidad,
            Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim(),
            UltimaFechaPago = dto.UltimaFechaPago,
            SaldoCapital = dto.SaldoCapital,
            FechaVencimiento = dto.FechaVencimiento,
            PlazoPago = dto.PlazoPago,
            UsuarioResponsable = dto.UsuarioResponsable.Trim(),
            HoraTransaccion = DateTime.UtcNow,
            MontoMaximo = dto.MontoMaximo
        });
    }

    public async Task<int> GetUltimaSecuenciaDetalleAsync(string codigoEmpresa, int codigoGrupo, string numeroContrato, CancellationToken cancellationToken)
    {
        return await _dbContext.DetallesContratos
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoEmpresa
                && x.CodigoGrupo == codigoGrupo
                && x.NumeroContrato == numeroContrato)
            .MaxAsync(x => (int?)x.SecuenciaContratos, cancellationToken) ?? 0;
    }

    public void AddDetalle(CrearEmpenioDetalleDto detalle, string codigoEmpresa, int codigoGrupo, string numeroContrato, int secuencia, int codigoCategoriaPrenda, int? controlReloj)
    {
        _dbContext.DetallesContratos.Add(new DetalleContratoDb
        {
            CodigoEmpresa = codigoEmpresa,
            CodigoGrupo = codigoGrupo,
            NumeroContrato = numeroContrato,
            SecuenciaContratos = secuencia,
            Descripcion = string.IsNullOrWhiteSpace(detalle.Descripcion) ? null : detalle.Descripcion.Trim(),
            Kilates = detalle.Kilataje,
            Peso = detalle.Peso,
            CodigoReloj = controlReloj,
            CantidadProducto = detalle.Cantidad ?? 1,
            CodigoCategoriaPrenda = codigoCategoriaPrenda
        });
    }

    public async Task<int> GetUltimoNumeroMovimientoAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        return await _dbContext.MovimientosCaja
            .AsNoTracking()
            .Where(x => x.CodigoEmpresa == codigoEmpresa)
            .MaxAsync(x => (int?)x.NumeroMovimiento, cancellationToken) ?? 0;
    }

    public void AddMovimientoCaja(CrearEmpenioContratoDto dto, int numeroMovimiento, string codigoTransaccion, DateTime horaTransaccion)
    {
        _dbContext.MovimientosCaja.Add(new MovimientoCajaDb
        {
            CodigoEmpresa = dto.CodigoEmpresa.Trim(),
            NumeroMovimiento = numeroMovimiento,
            CodigoTransaccion = codigoTransaccion,
            CodigoGrupo = dto.CodigoGrupo,
            NumeroContrato = dto.NumeroContrato.Trim(),
            MontoTransaccion = dto.CapitalPrestado,
            FechaTransaccion = dto.FechaCreacion,
            HoraTransaccion = horaTransaccion,
            MotivoTransaccion = "CREACION CONTRATO",
            UsuarioResponsable = dto.UsuarioResponsable.Trim()
        });
    }

    public void AddMovimientoTemporal(CrearEmpenioContratoDto dto, int numeroMovimiento, string codigoTransaccion, DateTime horaTransaccion)
    {
        _dbContext.MovimientosTemporales.Add(new MovimientoTemporalDb
        {
            CodigoEmpresa = dto.CodigoEmpresa.Trim(),
            NumeroMovimiento = numeroMovimiento,
            CodigoTransaccion = codigoTransaccion,
            CodigoGrupo = dto.CodigoGrupo,
            NumeroContrato = dto.NumeroContrato.Trim(),
            MontoTransaccion = dto.CapitalPrestado,
            FechaTransaccion = dto.FechaCreacion,
            HoraTransaccion = horaTransaccion,
            MotivoTransaccion = "CREACION CONTRATO",
            UsuarioResponsable = dto.UsuarioResponsable.Trim()
        });
    }

    public Task<bool> CategoriaPrendaExistsAsync(int codigoCategoriaPrenda, CancellationToken cancellationToken)
    {
        return _dbContext.CategoriasPrenda
            .AsNoTracking()
            .AnyAsync(x => x.CodigoCategoriaPrenda == codigoCategoriaPrenda, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}