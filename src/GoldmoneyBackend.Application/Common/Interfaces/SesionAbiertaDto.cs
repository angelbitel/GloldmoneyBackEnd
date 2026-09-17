namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record SesionAbiertaDto(
    string CodigoEmpresa,
    DateTime FechaApertura,
    decimal ValorCajaInicial);
