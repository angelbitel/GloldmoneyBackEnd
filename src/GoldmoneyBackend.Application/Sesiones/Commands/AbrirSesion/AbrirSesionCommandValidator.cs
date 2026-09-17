using FluentValidation;

namespace GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;

public sealed class AbrirSesionCommandValidator : AbstractValidator<AbrirSesionCommand>
{
    public AbrirSesionCommandValidator()
    {
        RuleFor(x => x.CodigoEmpresa)
            .NotEmpty()
            .MaximumLength(2);

        RuleFor(x => x.FechaApertura)
            .NotEmpty();

        RuleFor(x => x.UsuarioResponsable)
            .NotEmpty()
            .MaximumLength(14);
    }
}
