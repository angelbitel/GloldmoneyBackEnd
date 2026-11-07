using FluentValidation;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetContratosCobro;

public sealed class GetContratosCobroQueryValidator : AbstractValidator<GetContratosCobroQuery>
{
    public GetContratosCobroQueryValidator()
    {
        RuleFor(x => x.IdCliente)
            .NotEmpty()
            .MaximumLength(14);
    }
}
