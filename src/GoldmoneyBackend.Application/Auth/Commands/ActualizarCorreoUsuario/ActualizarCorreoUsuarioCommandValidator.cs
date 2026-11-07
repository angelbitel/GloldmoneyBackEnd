using FluentValidation;

namespace GoldmoneyBackend.Application.Auth.Commands.ActualizarCorreoUsuario;

public sealed class ActualizarCorreoUsuarioCommandValidator : AbstractValidator<ActualizarCorreoUsuarioCommand>
{
    public ActualizarCorreoUsuarioCommandValidator()
    {
        RuleFor(x => x.NombreUsuario)
            .NotEmpty()
            .MaximumLength(15);

        RuleFor(x => x.Correo!)
            .EmailAddress()
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Correo));
    }
}
