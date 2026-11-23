using GoldmoneyBackend.Application.Common.Interfaces;

namespace GoldmoneyBackend.Application.Empenios.Transacciones;

/// <summary>
/// Calcula el estado administrativo de un contrato a partir de las reglas de SADE.
/// Única fuente para /contratos-transaccion y /completo.
/// </summary>
public static class AdministracionContrato
{
    public static AdministracionContratoDto Construir(
        ContratoTransaccionDatos contrato,
        ParametrosTransaccionDto? parametros,
        SesionOperacionDto? sesion,
        DateTime ahora)
    {
        var motivo = ReglasTransaccionContrato.MotivoNoAnulable(contrato, sesion, parametros, ahora);

        return new AdministracionContratoDto(
            contrato.NombreEmpresa?.Trim(),
            contrato.AbreviaturaGrupo?.Trim().ToUpperInvariant(),
            ReglasTransaccionContrato.Estado(contrato.CantidadCopias),
            contrato.CantidadCopias,
            contrato.TienePagos,
            sesion?.FechaApertura,
            contrato.HoraTransaccion,
            parametros?.CostoCopia,
            motivo is null,
            motivo,
            parametros?.ControlarAnulacion ?? false);
    }

    /// <summary>Consulta lo necesario y construye el estado de un contrato; null si no existe.</summary>
    public static async Task<AdministracionContratoDto?> ConsultarAsync(
        ITransaccionesContratoRepository repository, ContratoClave clave, CancellationToken cancellationToken)
    {
        var normalizada = clave.Normalizada();
        var contrato = await repository.GetContratoAsync(normalizada, cancellationToken);
        if (contrato is null) return null;

        var parametros = await repository.GetParametrosAsync(normalizada.CodigoEmpresa, cancellationToken);
        var sesion = await repository.GetSesionOperacionAsync(normalizada.CodigoEmpresa, cancellationToken);
        var ahora = await repository.GetFechaServidorAsync(cancellationToken);

        return Construir(contrato, parametros, sesion, ahora);
    }
}
