using FluentValidation;

namespace GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

public sealed class CreateContratoCommandValidator : AbstractValidator<CreateContratoCommand>
{
    public CreateContratoCommandValidator()
    {
        RuleFor(x => x.ProcesoKey)
            .NotEmpty().WithMessage("Estimado usuario, no se ha indicado un tipo de proceso valido.");

        RuleFor(x => x.CodigoEmpresa)
            .NotEmpty().WithMessage("Estimado usuario, no ha seleccionado una empresa sobre la cual grabar los datos en pantalla.");

        RuleFor(x => x.IdCliente)
            .NotEmpty().When(x => x.ProcesoKey != "PagosContratos")
            .WithMessage("Estimado usuario, la identificacion del cliente o ID no es valido dentro del sistema.");

        RuleFor(x => x.FechaCreacion)
            .NotEmpty().WithMessage("Estimado usuario, la fecha con que se dispone grabar la transaccion no es valida dentro del sistema.")
            .Must(f => f.Year >= 1000).WithMessage("Estimado usuario, el ano introducido no puede contener menos de cuatro digitos.");

        RuleFor(x => x.CodigoGrupo)
            .GreaterThan(0).WithMessage("Estimado usuario, no ha seleccionado un grupo clasificatorio para el nuevo contrato a ingresar.");

        RuleFor(x => x.CapitalPrestado)
            .GreaterThan(0).WithMessage("Estimado usuario, no ha ingresado el monto para el nuevo contrato.");

        RuleFor(x => x.SaldoCapital)
            .GreaterThan(0).WithMessage("Estimado usuario, el saldo a capital del nuevo contrato debe ser mayor que cero.");

        RuleFor(x => x.NumeroContrato)
            .NotEmpty().WithMessage("Estimado usuario, no ha ingresado un numero de contrato valido para el contrato a ingresar.");

        RuleFor(x => x.Serie)
            .NotEmpty().WithMessage("Estimado usuario, no ha ingresado una serie valida para el contrato a ingresar.")
            .Must(s => s.Trim().Length >= 10 && s.Trim().Length <= 20)
            .WithMessage("Estimado usuario, verifique el numero de Contrato, ya que no cumple con el formato establecido para efectuar el empenio.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("No existe un nombre en pantalla para salvar en presente contrato.");

        RuleFor(x => x.Apellido)
            .NotEmpty().WithMessage("No existe un apellido en pantalla para salvar en presente contrato.");

        RuleFor(x => x.UsuarioResponsable)
            .NotEmpty().WithMessage("Estimado usuario, no se ha indicado un usuario responsable para la transaccion.");

        RuleFor(x => x.CodigoPais)
            .NotEmpty().WithMessage("Estimado usuario, no se ha indicado un codigo de pais valido para el cliente.");

        RuleFor(x => x.Detalles)
            .NotNull().WithMessage("Estimado usuario, debe ingresar al menos un detalle para el nuevo contrato.")
            .Must(d => d!.Count > 0).WithMessage("Estimado usuario, debe ingresar al menos un detalle para el nuevo contrato.");
    }
}