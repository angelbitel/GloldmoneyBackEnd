namespace GoldmoneyBackend.Application.Sesiones;

/// <summary>
/// INICIO_CIERRE_SESION.status_sesion según SADE: sp_Gravar_Sesiones_Empresa abre con 0 y el cierre
/// deja 1 junto con hora_cierre, usuario_cierre y valor_final_caja.
/// </summary>
public static class EstadoSesion
{
    public const decimal Abierta = 0m;
    public const decimal Cerrada = 1m;
}
