namespace GoldmoneyBackend.Api.Contracts.Pagos;

public sealed record RegistrarRetirosRequest(IReadOnlyList<RetiroContratoRequest> Contratos);

public sealed record RetiroContratoRequest(
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato);
