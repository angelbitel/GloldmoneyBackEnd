using FluentValidation;
using GoldmoneyBackend.Application.Auth.Commands.RestablecerContrasena;

namespace GoldmoneyBackend.Application.Auth.Commands.CambiarContrasenaUsuario;

public sealed class CambiarContrasenaUsuarioCommandValidator : AbstractValidator<CambiarContrasenaUsuarioCommand>
{
    public CambiarContrasenaUsuarioCommandValidator()
    {
        RuleFor(x => x.NombreUsuario)
            .NotEmpty()
            .MaximumLength(15);

        RuleFor(x => x.NuevaContrasena)
            .NotEmpty()
            .MinimumLength(RestablecerContrasenaCommandValidator.LongitudMinima)
            .MaximumLength(RestablecerContrasenaCommandValidator.LongitudMaxima)
            .Must(x => x == x.Trim())
            .WithMessage("La contraseña no puede empezar ni terminar con espacios.");
    }
}
