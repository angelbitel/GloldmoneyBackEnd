using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Empenios.Transacciones;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;

public sealed class GetContratoCompletoQueryHandler : IRequestHandler<GetContratoCompletoQuery, ContratoCompletoDto?>
{
    private readonly IEmpeniosReadService _empeniosReadService;
    private readonly ITransaccionesContratoRepository _transacciones;

    public GetContratoCompletoQueryHandler(
        IEmpeniosReadService empeniosReadService,
        ITransaccionesContratoRepository transacciones)
    {
        _empeniosReadService = empeniosReadService;
        _transacciones = transacciones;
    }

    public async Task<ContratoCompletoDto?> Handle(GetContratoCompletoQuery request, CancellationToken cancellationToken)
    {
        var completo = await _empeniosReadService.GetContratoCompletoByIdAsync(request.ContratoId, cancellationToken);
        if (completo is null) return null;

        // Se agrega el estado administrativo para poder editar el contrato (observación, copia, anulación).
        var clave = new ContratoClave(completo.CodigoEmpresa, completo.CodigoGrupo, completo.NumeroContrato);
        var administracion = await AdministracionContrato.ConsultarAsync(_transacciones, clave, cancellationToken);

        return completo with { Administracion = administracion };
    }
}
