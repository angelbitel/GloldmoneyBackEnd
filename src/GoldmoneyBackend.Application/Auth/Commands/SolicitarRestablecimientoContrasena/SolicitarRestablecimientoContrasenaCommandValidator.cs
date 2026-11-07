using FluentValidation;

namespace GoldmoneyBackend.Application.Auth.Commands.SolicitarRestablecimientoContrasena;

public sealed class SolicitarRestablecimientoContrasenaCommandValidator : AbstractValidator<SolicitarRestablecimientoContrasenaCommand>
{
    public SolicitarRestablecimientoContrasenaCommandValidator()
    {
        RuleFor(x => x.UsuarioOCorreo)
            .NotEmpty()
            .MaximumLength(256);
    }
}
