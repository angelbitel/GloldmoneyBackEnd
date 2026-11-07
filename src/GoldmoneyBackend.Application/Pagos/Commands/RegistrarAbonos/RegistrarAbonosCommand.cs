using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarAbonos;

/// <summary>
/// Registra abonos sobre uno o varios contratos. Cada contrato se graba en su propia
/// transacción: si uno falla, los demás se registran igualmente.
/// </summary>
public sealed record RegistrarAbonosCommand(
    IReadOnlyList<AbonoContratoItem> Contratos,
    string UsuarioResponsable) : IRequest<IReadOnlyList<AbonoRegistradoDto>>;

/// <param name="Cuotas">Cuotas de interés a pagar ("meses a pagar"); puede adelantar cuotas.</param>
/// <param name="AbonoCapital">Abono a capital; solo se admite si las cuotas cubren lo que debe.</param>
public sealed record AbonoContratoItem(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    int Cuotas,
    decimal AbonoCapital);
