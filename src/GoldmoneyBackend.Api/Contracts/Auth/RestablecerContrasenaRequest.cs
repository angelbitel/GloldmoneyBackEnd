namespace GoldmoneyBackend.Api.Contracts.Auth;

public sealed record RestablecerContrasenaRequest(string Token, string NuevaContrasena);
