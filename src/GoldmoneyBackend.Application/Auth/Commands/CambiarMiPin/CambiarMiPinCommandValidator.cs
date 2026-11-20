using FluentValidation;

namespace GoldmoneyBackend.Application.Auth.Commands.CambiarMiPin;

public sealed class CambiarMiPinCommandValidator : AbstractValidator<CambiarMiPinCommand>
{
    public CambiarMiPinCommandValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().MaximumLength(15);
        RuleFor(x => x.Contrasena).NotEmpty();
        RuleFor(x => x.Pin).EsPinValido();
    }
}
