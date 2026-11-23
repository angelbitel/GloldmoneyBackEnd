using GoldmoneyBackend.Domain.Common;

namespace GoldmoneyBackend.Application.Empenios.Transacciones;

/// <summary>
/// Reglas de negocio de SADE (Seleccionar_Copia_o_Anulación_Recibo, Salvar_Anulacion_Recibo,
/// Salvar_Copias_Recibo, SalvarObservacionContrato) sin acceso a datos, para poder probarlas.
/// </summary>
public static class ReglasTransaccionContrato
{
    public const int LongitudMaximaObservacion = 80;
    public const string EstadoOriginal = "Original";
    public const string EstadoCopia = "Copia";

    public static string Estado(int cantidadCopias) => cantidadCopias > 0 ? EstadoCopia : EstadoOriginal;

    public static string? NormalizarObservacion(string? observacion)
    {
        var valor = observacion?.Trim();
        if (string.IsNullOrEmpty(valor)) return null;

        if (valor.Length > LongitudMaximaObservacion)
            throw new DomainValidationException($"Estimado usuario, la observación no puede superar los {LongitudMaximaObservacion} caracteres.");

        return valor;
    }

    public static SesionOperacionDto ExigirSesion(SesionOperacionDto? sesion) =>
        sesion ?? throw new DomainValidationException(
            "Estimado usuario, la sesión de la empresa a la cual pertenece el contrato no ha sido iniciada, por tal motivo no es posible concluir la transacción.");

    public static ParametrosTransaccionDto ExigirParametros(ParametrosTransaccionDto? parametros) =>
        parametros ?? throw new DomainValidationException("Estimado usuario, no se han establecido los parámetros de la empresa.");

    public static decimal CostoCopia(ParametrosTransaccionDto parametros) =>
        parametros.CostoCopia ?? throw new DomainValidationException(
            "Estimado usuario, no se ha localizado una cantidad de dinero correspondiente al costo de la copia.");

    /// <summary>
    /// Devuelve el motivo por el que el contrato no puede anularse, o null si cumple todas las reglas
    /// (excepto la clave de supervisor, que se valida aparte).
    /// </summary>
    public static string? MotivoNoAnulable(
        ContratoTransaccionDatos contrato,
        SesionOperacionDto? sesion,
        ParametrosTransaccionDto? parametros,
        DateTime ahora)
    {
        var etiqueta = string.IsNullOrWhiteSpace(contrato.Serie) ? contrato.NumeroContrato : contrato.Serie.Trim();

        if (sesion is null)
            return "La sesión de la empresa a la cual pertenece el contrato no ha sido iniciada.";

        if (parametros is null)
            return "No se han establecido los parámetros de la empresa.";

        if (contrato.FechaCreacion?.Date != sesion.FechaApertura.Date)
            return $"El contrato [{etiqueta}] no fue creado en la fecha en la que actualmente está trabajando la empresa ({sesion.FechaApertura:dd/MM/yyyy}); solo se anulan contratos del día.";

        if (contrato.TienePagos)
            return $"El contrato [{etiqueta}] tiene registrados abonos a interés, por lo que no es posible anularlo.";

        if (SobrepasaTiempoAnulacion(contrato.HoraTransaccion, parametros, ahora))
            return $"Se sobrepasó el tiempo máximo permitido ({parametros.TiempoAnulacionMinutos} min) para efectuar la anulación.";

        return null;
    }

    /// <summary>
    /// SADE compara solo la hora del día: hora_transaccion + tiempo_anulacion contra la hora actual.
    /// </summary>
    public static bool SobrepasaTiempoAnulacion(DateTime? horaTransaccion, ParametrosTransaccionDto parametros, DateTime ahora)
    {
        if (!parametros.AnulacionControladaTiempo || horaTransaccion is null) return false;

        var minutos = parametros.TiempoAnulacionMinutos ?? 0;
        var limite = horaTransaccion.Value.TimeOfDay + TimeSpan.FromMinutes(minutos);

        // Si el límite pasa de medianoche, el contrato sigue anulable el resto del día.
        if (limite >= TimeSpan.FromDays(1)) return false;

        return ahora.TimeOfDay > limite;
    }

    public static void ValidarAnulacion(
        ContratoTransaccionDatos contrato,
        SesionOperacionDto? sesion,
        ParametrosTransaccionDto? parametros,
        DateTime ahora)
    {
        var motivo = MotivoNoAnulable(contrato, sesion, parametros, ahora);
        if (motivo is not null)
            throw new DomainValidationException($"Estimado usuario, {char.ToLowerInvariant(motivo[0])}{motivo[1..]}");
    }
}
