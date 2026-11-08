using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using GoldmoneyBackend.Infrastructure.Persistence.Cobros;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Infrastructure.Persistence.Repositories;

public sealed class CobrosRepository : ICobrosRepository
{
    private const string MensajeReciboRepetido = "Alguno de los recibos ya tiene registrada su forma de pago.";

    private readonly ApplicationDbContext _dbContext;

    public CobrosRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CobroDto> RegistrarAsync(RegistroCobroDto cobro, CancellationToken cancellationToken)
    {
        foreach (var documento in cobro.Documentos)
        {
            var yaRegistrado = await _dbContext.CobrosDocumento.AnyAsync(
                x => x.CodigoEmpresa == documento.CodigoEmpresa && x.Tipo == documento.Tipo && x.NumeroRecibo == documento.NumeroRecibo,
                cancellationToken);
            if (yaRegistrado)
            {
                throw new ConflictDomainException(MensajeReciboRepetido);
            }
        }

        var entidad = new Cobro
        {
            Id = Guid.NewGuid(),
            FechaUtc = cobro.FechaUtc,
            Usuario = cobro.Usuario,
            IdCliente = cobro.IdCliente,
            TotalMetodos = cobro.Metodos.Sum(x => x.Monto),
            TotalDocumentos = cobro.Documentos.Sum(x => x.Total),
            Metodos = cobro.Metodos.Select(x => new CobroMetodoPago { Metodo = x.Metodo, Monto = x.Monto }).ToList(),
            Documentos = cobro.Documentos.Select(x => new CobroDocumento
            {
                Tipo = x.Tipo,
                CodigoEmpresa = x.CodigoEmpresa,
                CodigoGrupo = x.CodigoGrupo,
                NumeroContrato = x.NumeroContrato,
                NumeroRecibo = x.NumeroRecibo,
                Total = x.Total
            }).ToList()
        };

        _dbContext.Cobros.Add(entidad);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        // Dos peticiones con el mismo recibo a la vez: el índice único rechaza la segunda.
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _dbContext.ChangeTracker.Clear();
            throw new ConflictDomainException(MensajeReciboRepetido);
        }

        return ADto(entidad);
    }

    public async Task<CobroDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entidad = await _dbContext.Cobros
            .AsNoTracking()
            .Include(x => x.Metodos)
            .Include(x => x.Documentos)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entidad is null ? null : ADto(entidad);
    }

    private static CobroDto ADto(Cobro cobro) => new(
        cobro.Id,
        cobro.FechaUtc,
        cobro.Usuario,
        cobro.IdCliente,
        cobro.TotalMetodos,
        cobro.TotalDocumentos,
        cobro.Metodos.Select(x => new MetodoPagoCobroDto(x.Metodo, x.Monto)).ToList(),
        cobro.Documentos
            .Select(x => new DocumentoCobroDto(x.Tipo, x.CodigoEmpresa, x.CodigoGrupo, x.NumeroContrato, x.NumeroRecibo, x.Total))
            .ToList());
}
