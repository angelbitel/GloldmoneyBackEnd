using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Transacciones;

public sealed record AnularContratoCommand(
    ContratoClave Contrato,
    string UsuarioResponsable,
    string? SupervisorUsuario,
    string? SupervisorClave) : IRequest<AnularContratoResultDto>;

/// <summary>Equivale a Salvar_Anulacion_Recibo + sp_ad_Efectuar_Anulacion_Contrato de SADE.</summary>
public sealed class AnularContratoCommandHandler : IRequestHandler<AnularContratoCommand, AnularContratoResultDto>
{
    private readonly ITransaccionesContratoRepository _repository;

    public AnularContratoCommandHandler(ITransaccionesContratoRepository repository)
    {
        _repository = repository;
    }

    public async Task<AnularContratoResultDto> Handle(AnularContratoCommand request, CancellationToken cancellationToken)
    {
        var clave = request.Contrato.Normalizada();
        var usuario = Usuario.Normalizar(request.UsuarioResponsable);

        var contrato = await _repository.GetContratoAsync(clave, cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, el número de contrato no existe en la base de datos.");

        var sesion = await _repository.GetSesionOperacionAsync(clave.CodigoEmpresa, cancellationToken);
        var parametros = await _repository.GetParametrosAsync(clave.CodigoEmpresa, cancellationToken);
        var ahora = await _repository.GetFechaServidorAsync(cancellationToken);

        ReglasTransaccionContrato.ValidarAnulacion(contrato, sesion, parametros, ahora);

        if (parametros!.ControlarAnulacion)
        {
            if (string.IsNullOrWhiteSpace(request.SupervisorUsuario) || string.IsNullOrEmpty(request.SupervisorClave))
                throw new DomainValidationException("Estimado usuario, la anulación de contratos requiere la clave de un supervisor.");

            var autorizado = await _repository.ValidarSupervisorAnulacionAsync(request.SupervisorUsuario.Trim(), request.SupervisorClave, cancellationToken);
            if (!autorizado)
                throw new DomainValidationException("Estimado usuario, el usuario indicado no tiene acceso al proceso de anulación de contratos o la clave es incorrecta.");
        }

        await _repository.AnularContratoAsync(
            new AnularContratoDatos(clave, sesion!.CodigoEmpresaCaja, sesion.FechaApertura.Date, ahora, usuario),
            cancellationToken);

        return new AnularContratoResultDto(clave.CodigoEmpresa, clave.CodigoGrupo, clave.NumeroContrato, contrato.CapitalPrestado);
    }
}
