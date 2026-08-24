using FluentValidation;

namespace GoldmoneyBackend.Application.Clientes.Commands.DeleteCliente;

public sealed class DeleteClienteCommandValidator : AbstractValidator<DeleteClienteCommand>
{
    public DeleteClienteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}