using FluentValidation;

namespace GoldmoneyBackend.Application.Auth.Commands.RestablecerContrasena;

public sealed class RestablecerContrasenaCommandValidator : AbstractValidator<RestablecerContrasenaCommand>
{
    /// <summary>Tamaño de USUARIOS.contrasena, que comparte con el VB6.</summary>
    public const int LongitudMaxima = 25;
    public const int LongitudMinima = 6;

    public RestablecerContrasenaCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.NuevaContrasena)
            .NotEmpty()
            .MinimumLength(LongitudMinima)
            .MaximumLength(LongitudMaxima)
            .Must(x => x == x.Trim())
            .WithMessage("La contraseña no puede empezar ni terminar con espacios.");
    }
}
