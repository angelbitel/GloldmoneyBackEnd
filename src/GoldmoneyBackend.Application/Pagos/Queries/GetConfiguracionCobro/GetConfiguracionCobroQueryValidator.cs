using FluentValidation;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetConfiguracionCobro;

public sealed class GetConfiguracionCobroQueryValidator : AbstractValidator<GetConfiguracionCobroQuery>
{
    public GetConfiguracionCobroQueryValidator()
    {
        RuleFor(x => x.CodigoEmpresa)
            .NotEmpty()
            .MaximumLength(2);
    }
}
