namespace GoldmoneyBackend.Application.Empenios.Queries.GetContratoById;

public sealed record ContratoDto(
    string ContratoId,
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    string? IdCliente,
    DateTime FechaCreacion,
    decimal CapitalPrestado,
    decimal SaldoCapital,
    string? UsuarioResponsable);
