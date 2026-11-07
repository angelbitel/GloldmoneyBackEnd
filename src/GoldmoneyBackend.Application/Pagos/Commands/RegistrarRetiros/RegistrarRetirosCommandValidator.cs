using FluentValidation;

namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarRetiros;

public sealed class RegistrarRetirosCommandValidator : AbstractValidator<RegistrarRetirosCommand>
{
    public RegistrarRetirosCommandValidator()
    {
        RuleFor(x => x.UsuarioResponsable)
            .NotEmpty()
            .MaximumLength(14);

        RuleFor(x => x.Contratos)
            .NotEmpty()
            .Must(contratos => contratos
                .Select(c => (c.CodigoEmpresa?.Trim(), c.CodigoGrupo, c.NumeroContrato?.Trim()))
                .Distinct()
                .Count() == contratos.Count)
            .WithMessage("Un contrato no puede aparecer más de una vez en el mismo cobro.");

        RuleForEach(x => x.Contratos).ChildRules(contrato =>
        {
            contrato.RuleFor(c => c.CodigoEmpresa).NotEmpty().MaximumLength(2);
            contrato.RuleFor(c => c.CodigoGrupo).GreaterThan(0);
            contrato.RuleFor(c => c.NumeroContrato).NotEmpty().MaximumLength(20);
        });
    }
}
