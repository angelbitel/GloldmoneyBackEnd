namespace GoldmoneyBackend.Api.Contracts.ValorDelOro;

public sealed record CreateValorDelOroRequest(
    string CodigoEmpresa,
    string StatusCalidad,
    decimal Kilataje,
    decimal? MaximoValor,
    decimal? MinimoValor);
