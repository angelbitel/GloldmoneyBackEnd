using FluentValidation;

namespace GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

/// <summary>Tamaños según las columnas de CONTRATOS, CLIENTES y DETALLES_CONTRATOS.</summary>
public sealed class CreateContratoCommandValidator : AbstractValidator<CreateContratoCommand>
{
    public CreateContratoCommandValidator()
    {
        RuleFor(x => x.CodigoEmpresa).NotEmpty().MaximumLength(2);
        RuleFor(x => x.CodigoGrupo).GreaterThan(0);
        RuleFor(x => x.IdCliente).NotEmpty().MaximumLength(14);
        RuleFor(x => x.Nombre).MaximumLength(20);
        RuleFor(x => x.Apellido).MaximumLength(20);
        RuleFor(x => x.Direccion).MaximumLength(40);
        RuleFor(x => x.Telefono).MaximumLength(20);
        RuleFor(x => x.CodigoPais).MaximumLength(5);
        RuleFor(x => x.UsuarioResponsable).NotEmpty().MaximumLength(14);

        RuleFor(x => x.CapitalPrestado)
            .GreaterThan(0)
            .PrecisionScale(15, 2, true);

        RuleFor(x => x.Detalles)
            .NotEmpty()
            .WithMessage("Estimado usuario, debe ingresar al menos un detalle para el nuevo contrato.");

        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(x => x.CodigoTipoPrenda).GreaterThan(0)
                .WithMessage("Estimado usuario, cada detalle debe contener un codigo de tipo de prenda valido.");
            detalle.RuleFor(x => x.Descripcion).MaximumLength(300);
            detalle.RuleFor(x => x.Peso).GreaterThanOrEqualTo(0).PrecisionScale(6, 2, true);
            detalle.RuleFor(x => x.Kilataje).GreaterThanOrEqualTo(0).PrecisionScale(2, 0, true);
            detalle.RuleFor(x => x.Cantidad).GreaterThan(0);
        });
    }
}
