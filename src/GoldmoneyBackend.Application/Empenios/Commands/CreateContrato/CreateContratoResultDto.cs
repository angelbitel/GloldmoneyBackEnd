namespace GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

/// <param name="ContratoId">Código de barras del contrato: empresa + grupo + número.</param>
public sealed record CreateContratoResultDto(
    string ContratoId,
    string CodigoEmpresa,
    int CodigoGrupo,
    string NumeroContrato,
    string Serie,
    DateTime FechaCreacion,
    DateTime FechaVencimiento,
    decimal CapitalPrestado,
    decimal TasaInteres,
    decimal InteresMensual,
    int PlazoMeses);
