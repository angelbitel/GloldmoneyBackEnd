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

    public Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancellationToken) =>
        OperacionesCajaLegacy.EnTransaccionAsync(_dbContext, operacion, cancellationToken);

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

    public async Task<GrupoEmpenioDto?> GetGrupoAsync(string codigoEmpresa, int codigoGrupo, CancellationToken cancellationToken)
    {
        var grupo = await _dbContext.Grupos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CodigoEmpresa == codigoEmpresa && x.CodigoGrupo == codigoGrupo, cancellationToken);

        if (grupo is null) return null;

        return new GrupoEmpenioDto(
            grupo.CodigoEmpresa,
            grupo.CodigoGrupo,
            grupo.AbreviaturaGrupo,
            grupo.TasaInteres ?? 0m,
            (int)(grupo.MesesPlazo ?? 0m),
            // GRUPOS.estado_grupo: 1 = activo.
            grupo.EstadoGrupo == 1);
    }

    public async Task<DateTime?> GetFechaSesionAbiertaAsync(string codigoEmpresa, CancellationToken cancellationToken)
    {
        var (fechaSesion, _) = await OperacionesCajaLegacy.GetSesionAbiertaAsync(_dbContext, codigoEmpresa, cancellationToken);
        return fechaSesion;
    }

    public Task<int> SiguienteSecuenciaGrupoAsync(string codigoEmpresa, string abreviaturaGrupo, CancellationToken cancellationToken) =>
        OperacionesCajaLegacy.SiguienteSecuenciaAsync(_dbContext, codigoEmpresa, abreviaturaGrupo, cancellationToken);

    public Task DescontarCajaAsync(string codigoEmpresa, decimal monto, CancellationToken cancellationToken) =>
        OperacionesCajaLegacy.ActualizarCajaEmpresaAsync(_dbContext, codigoEmpresa.Trim(), -monto, cancellationToken, exigirSaldo: true);

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
        // Mismos valores que un contrato del VB6 recién creado: los saldos y el monto máximo
        // empiezan en el capital y no lleva observación.
        _dbContext.Contratos.Add(new ContratoDb
        {
            CodigoEmpresa = dto.CodigoEmpresa,
            CodigoGrupo = dto.CodigoGrupo,
            NumeroContrato = dto.NumeroContrato,
            IdCliente = dto.IdCliente,
            Serie = dto.Serie,
            FechaCreacion = dto.FechaCreacion,
            CapitalPrestado = dto.CapitalPrestado,
            Interes = dto.TasaInteres,
            SaldoActual = dto.CapitalPrestado,
            InteresMensual = dto.InteresMensual,
            Observacion = null,
            UltimaFechaPago = dto.UltimaFechaPago,
            SaldoCapital = dto.CapitalPrestado,
            FechaVencimiento = dto.FechaVencimiento,
            PlazoPago = dto.PlazoMeses,
            UsuarioResponsable = dto.UsuarioResponsable,
            // Fecha y hora local completas, como el VB6.
            HoraTransaccion = OperacionesCajaLegacy.AhoraLocal(),
            MontoMaximo = dto.CapitalPrestado
        });
    }

    public void AddDetalle(CrearEmpenioDetalleDto detalle, string codigoEmpresa, int codigoGrupo, string numeroContrato, int secuencia)
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
            // El VB6 guarda 0 en todos los detalles.
            CodigoReloj = 0,
            CantidadProducto = detalle.Cantidad,
            CodigoCategoriaPrenda = detalle.CodigoTipoPrenda
        });
    }

    public async Task AgregarMovimientoCajaAsync(CrearEmpenioContratoDto dto, string codigoTransaccion, CancellationToken cancellationToken)
    {
        // Igual que el VB6: número de SEC_CAJA, fecha de la sesión (la del contrato), hora sobre
        // el 1900-01-01 y motivo 'NULL'.
        await OperacionesCajaLegacy.AgregarMovimientoAsync(
            _dbContext,
            dto.CodigoEmpresa.Trim(),
            dto.CodigoGrupo,
            dto.NumeroContrato.Trim(),
            codigoTransaccion,
            dto.CapitalPrestado,
            dto.FechaCreacion.Date,
            OperacionesCajaLegacy.AhoraLocal(),
            dto.UsuarioResponsable.Trim(),
            cancellationToken);
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