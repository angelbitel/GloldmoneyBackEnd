namespace GoldmoneyBackend.Api.Contracts.Auth;

/// <param name="Contrasena">Contraseña actual del usuario, para confirmar que es él.</param>
/// <param name="Pin">De 4 a 6 dígitos.</param>
public sealed record CambiarMiPinRequest(string Contrasena, string Pin);
