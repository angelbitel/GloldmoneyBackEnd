using FluentValidation;

namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarFormaPago;

public sealed class RegistrarFormaPagoCommandValidator : AbstractValidator<RegistrarFormaPagoCommand>
{
    public RegistrarFormaPagoCommandValidator()
    {
        // Mismo tamaño que CLIENTES.id_cliente.
        RuleFor(x => x.IdCliente).NotEmpty().MaximumLength(14);
        RuleFor(x => x.UsuarioResponsable).NotEmpty().MaximumLength(50);

        RuleFor(x => x.Metodos)
            .NotEmpty()
            .Must(m => m.Select(x => x.Metodo).Distinct().Count() == m.Count)
            .WithMessage("Cada método de pago debe aparecer una sola vez.");

        RuleForEach(x => x.Metodos).ChildRules(metodo =>
        {
            metodo.RuleFor(x => x.Metodo)
                .Must(MetodosPago.Validos.Contains)
                .WithMessage($"Método de pago no válido. Use: {string.Join(", ", MetodosPago.Validos)}.");
            metodo.RuleFor(x => x.Monto).GreaterThan(0).PrecisionScale(18, 2, true);
        });

        RuleFor(x => x.Documentos)
            .NotEmpty()
            .Must(d => d.Select(x => (x.Tipo, x.CodigoEmpresa, x.NumeroRecibo)).Distinct().Count() == d.Count)
            .WithMessage("Un recibo no puede aparecer dos veces en el mismo cobro.");

        RuleForEach(x => x.Documentos).ChildRules(documento =>
        {
            documento.RuleFor(x => x.Tipo)
                .Must(MetodosPago.TiposDocumento.Contains)
                .WithMessage("El tipo de documento debe ser ABONO o RETIRO.");
            // Mismos tamaños que CONTRATOS.codigo_empresa y numero_contrato.
            documento.RuleFor(x => x.CodigoEmpresa).NotEmpty().MaximumLength(2);
            documento.RuleFor(x => x.NumeroContrato).NotEmpty().MaximumLength(20);
            documento.RuleFor(x => x.NumeroRecibo).GreaterThan(0);
            documento.RuleFor(x => x.Total).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
        });
    }
}
