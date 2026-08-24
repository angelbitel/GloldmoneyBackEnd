namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record EmpresaDbUpsertDto(
    string CodigoEmpresa,
    string? NombreEmpresa,
    string? Direccion,
    string? Ruc,
    string? Telefono,
    decimal? MontoInicial,
    decimal? MontoAuxiliar,
    int? ManejoCajaDep,
    int? CodEmpresaCaja);
