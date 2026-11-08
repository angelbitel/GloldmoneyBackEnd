using FluentValidation;

namespace GoldmoneyBackend.Application.Clientes.Commands.ActualizarComentarioCliente;

public sealed class ActualizarComentarioClienteCommandValidator : AbstractValidator<ActualizarComentarioClienteCommand>
{
    /// <summary>Tamaño de CLIENTES.comentario.</summary>
    public const int LongitudMaxima = 300;

    public ActualizarComentarioClienteCommandValidator()
    {
        RuleFor(x => x.IdCliente).NotEmpty().MaximumLength(14);

        RuleFor(x => x.Comentario)
            .Must(x => x is null || x.Trim().Length <= LongitudMaxima)
            .WithMessage($"La nota no puede pasar de {LongitudMaxima} caracteres.");
    }
}
