using FluentValidation;

namespace GoldmoneyBackend.Application.Clientes.Commands.UpdateCliente;

public sealed class UpdateClienteCommandValidator : AbstractValidator<UpdateClienteCommand>
{
    public UpdateClienteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Nombre)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Documento)
            .NotEmpty()
            .Length(5, 20);
    }
}