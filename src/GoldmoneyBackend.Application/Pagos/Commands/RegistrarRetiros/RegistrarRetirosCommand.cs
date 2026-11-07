using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Commands.RegistrarRetiros;

/// <summary>
/// Retira uno o varios contratos cobrando su saldo de capital e interés a la fecha de sesión.
/// Cada contrato se graba en su propia transacción: si uno falla, los demás se retiran igualmente.
/// </summary>
public sealed record RegistrarRetirosCommand(
    IReadOnlyList<RetiroContratoItem> Contratos,
    string UsuarioResponsable) : IRequest<IReadOnlyList<RetiroRegistradoDto>>;

public sealed record RetiroContratoItem(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato);
