namespace GoldmoneyBackend.Api.Contracts.Empenios;

public sealed record ActualizarObservacionRequest(string? Observacion);

public sealed record AnularContratoRequest(string? SupervisorUsuario, string? SupervisorClave);
