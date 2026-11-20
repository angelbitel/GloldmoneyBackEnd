using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;

public sealed class AbrirSesionCommandHandler : IRequestHandler<AbrirSesionCommand, SesionAbiertaDto>
{
    private readonly ISesionesRepository _sesionesRepository;
    private readonly ICuentasUsuarioService _cuentas;

    public AbrirSesionCommandHandler(ISesionesRepository sesionesRepository, ICuentasUsuarioService cuentas)
    {
        _sesionesRepository = sesionesRepository;
        _cuentas = cuentas;
    }

    public async Task<SesionAbiertaDto> Handle(AbrirSesionCommand request, CancellationToken cancellationToken)
    {
        var usuario = request.UsuarioResponsable.Trim();

        switch (await _cuentas.VerificarPinAsync(usuario, request.Pin, cancellationToken))
        {
            case ResultadoPin.SinPin:
                throw new DomainValidationException("Su usuario no tiene PIN. Defínalo en «Mi PIN» del menú de perfil antes de abrir la caja.");
            case ResultadoPin.Incorrecto:
                throw new DomainValidationException("El PIN no es correcto.");
        }

        var dto = new AbrirSesionDto(request.CodigoEmpresa.Trim(), usuario);
        return await _sesionesRepository.AbrirSesionAsync(dto, cancellationToken);
    }
}
