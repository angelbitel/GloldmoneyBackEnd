using FluentValidation;

namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarAbonos;

public sealed class RegistrarAbonosCommandValidator : AbstractValidator<RegistrarAbonosCommand>
{
    // Límites de las columnas legacy: cantidad_pagos y pago_capital son decimal(8,2).
    private const int MaximoCuotas = 999;
    private const decimal MaximoImporte = 999_999.99m;

    public RegistrarAbonosCommandValidator()
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
            contrato.RuleFor(c => c.Cuotas).InclusiveBetween(0, MaximoCuotas);
            contrato.RuleFor(c => c.AbonoCapital)
                .InclusiveBetween(0m, MaximoImporte)
                .PrecisionScale(8, 2, ignoreTrailingZeros: true);
            contrato.RuleFor(c => c)
                .Must(c => c.Cuotas > 0 || c.AbonoCapital > 0)
                .WithName("Contrato")
                .WithMessage("Indique cuotas a pagar o un abono a capital.");
        });
    }
}
