using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Transacciones;

public sealed record GenerarCopiaReciboCommand(ContratoClave Contrato, string UsuarioResponsable) : IRequest<CopiaReciboResultDto>;

/// <summary>Equivale a Salvar_Copias_Recibo de SADE.</summary>
public sealed class GenerarCopiaReciboCommandHandler : IRequestHandler<GenerarCopiaReciboCommand, CopiaReciboResultDto>
{
    private readonly ITransaccionesContratoRepository _repository;

    public GenerarCopiaReciboCommandHandler(ITransaccionesContratoRepository repository)
    {
        _repository = repository;
    }

    public async Task<CopiaReciboResultDto> Handle(GenerarCopiaReciboCommand request, CancellationToken cancellationToken)
    {
        var clave = request.Contrato.Normalizada();

        var contrato = await _repository.GetContratoAsync(clave, cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, el número de contrato no existe en la base de datos.");

        var sesion = ReglasTransaccionContrato.ExigirSesion(await _repository.GetSesionOperacionAsync(clave.CodigoEmpresa, cancellationToken));
        var parametros = ReglasTransaccionContrato.ExigirParametros(await _repository.GetParametrosAsync(clave.CodigoEmpresa, cancellationToken));
        var monto = ReglasTransaccionContrato.CostoCopia(parametros);
        var ahora = await _repository.GetFechaServidorAsync(cancellationToken);
        var usuario = Usuario.Normalizar(request.UsuarioResponsable);

        var registrada = await _repository.RegistrarCopiaReciboAsync(
            new RegistrarCopiaReciboDatos(clave, contrato.IdCliente.Trim(), sesion.CodigoEmpresaCaja, sesion.FechaApertura.Date, ahora, monto, usuario),
            cancellationToken);

        return new CopiaReciboResultDto(
            clave.CodigoEmpresa, clave.CodigoGrupo, clave.NumeroContrato, contrato.Serie?.Trim(), contrato.IdCliente.Trim(),
            registrada.NumeroCopia, registrada.SecuenciaRecibo, monto, sesion.FechaApertura.Date, usuario);
    }
}

internal static class Usuario
{
    // usuario_responsable es varchar(14) en todas las tablas legacy.
    public static string Normalizar(string usuario)
    {
        var valor = usuario?.Trim() ?? string.Empty;
        if (valor.Length == 0)
            throw new DomainValidationException("Estimado usuario, no se encontró el usuario responsable de la transacción.");

        return valor.Length > 14 ? valor[..14] : valor;
    }
}
