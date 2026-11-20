namespace GoldmoneyBackend.Api.Contracts.Usuarios;

/// <param name="Pin">De 4 a 6 dígitos.</param>
public sealed record AsignarPinUsuarioRequest(string Pin);
