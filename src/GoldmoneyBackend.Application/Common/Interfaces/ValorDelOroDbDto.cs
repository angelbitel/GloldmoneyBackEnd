namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record ValorDelOroDbDto(
    string CodigoEmpresa,
    string StatusCalidad,
    decimal Kilataje,
    decimal? MaximoValor,
    decimal? MinimoValor);
