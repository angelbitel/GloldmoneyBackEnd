using FluentValidation;

namespace GoldmoneyBackend.Application.Clientes.Commands.CreateCliente;

public sealed class CreateClienteCommandValidator : AbstractValidator<CreateClienteCommand>
{
    public CreateClienteCommandValidator()
    {
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