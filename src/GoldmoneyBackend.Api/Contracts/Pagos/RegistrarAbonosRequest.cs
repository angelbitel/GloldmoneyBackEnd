namespace GoldmoneyBackend.Api.Contracts.Pagos;

public sealed record RegistrarAbonosRequest(IReadOnlyList<AbonoContratoRequest> Contratos);

public sealed record AbonoContratoRequest(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    int Cuotas,
    decimal AbonoCapital);
