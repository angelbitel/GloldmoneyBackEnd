using FluentValidation;

namespace GoldmoneyBackend.Application.Auth.Commands.AsignarPinUsuario;

public sealed class AsignarPinUsuarioCommandValidator : AbstractValidator<AsignarPinUsuarioCommand>
{
    public AsignarPinUsuarioCommandValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().MaximumLength(15);
        RuleFor(x => x.Pin).EsPinValido();
    }
}
