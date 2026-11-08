using FluentValidation;

namespace GoldmoneyBackend.Application.Auth.Commands.RestablecerContrasena;

public sealed class RestablecerContrasenaCommandValidator : AbstractValidator<RestablecerContrasenaCommand>
{
    /// <summary>
    /// Límites de la contraseña del sistema nuevo, que se guarda como hash en UsuariosCredencial
    /// (no en USUARIOS.contrasena, así que no depende de sus 25 caracteres).
    /// </summary>
    public const int LongitudMaxima = 128;
    public const int LongitudMinima = 8;

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
