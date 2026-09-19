namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record EmpresaCajaDto(
    string CodigoEmpresa,
    decimal? MontoInicial,
    decimal? MontoAuxiliar,
    int? ManejoCajaDep);