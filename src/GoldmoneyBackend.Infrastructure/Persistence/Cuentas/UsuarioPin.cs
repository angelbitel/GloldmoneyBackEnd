namespace GoldmoneyBackend.Infrastructure.Persistence.Cuentas;

/// <summary>
/// PIN del usuario para confirmar operaciones de caja (por ahora, la apertura), como hash
/// (ver HasherContrasenas). Va aparte de USUARIOS para no alterar la tabla que escribe el VB6.
/// </summary>
public sealed class UsuarioPin
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string PinHash { get; set; } = string.Empty;
    public DateTime ActualizadoEnUtc { get; set; }
}
