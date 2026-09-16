namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record ValorDelOroDbUpsertDto(
    string CodigoEmpresa,
    string StatusCalidad,
    decimal Kilataje,
    decimal? MaximoValor,
    decimal? MinimoValor);
