namespace GoldmoneyBackend.Api.Contracts.ValorDelOro;

public sealed record UpdateValorDelOroRequest(
    decimal? MaximoValor,
    decimal? MinimoValor);
