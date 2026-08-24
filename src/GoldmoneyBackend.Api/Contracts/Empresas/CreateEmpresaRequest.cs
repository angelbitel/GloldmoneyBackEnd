namespace GoldmoneyBackend.Api.Contracts.Empresas;

public sealed record CreateEmpresaRequest(
    string CodigoEmpresa,
    string? NombreEmpresa,
    string? Direccion,
    string? Ruc,
    string? Telefono,
    decimal? MontoInicial,
    decimal? MontoAuxiliar,
    int? ManejoCajaDep,
    int? CodEmpresaCaja);
