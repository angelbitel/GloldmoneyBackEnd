using FluentValidation;

namespace GoldmoneyBackend.Application.Auth;

public static class ReglasPin
{
    /// <summary>PIN de 4 a 6 dígitos.</summary>
    public static IRuleBuilderOptions<T, string> EsPinValido<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty()
            .Matches("^[0-9]{4,6}$")
            .WithMessage("El PIN debe tener de 4 a 6 dígitos.");
}
