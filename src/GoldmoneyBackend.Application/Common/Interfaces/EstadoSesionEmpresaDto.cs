namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record EstadoSesionEmpresaDto(
    string CodigoEmpresa,
    string? NombreEmpresa,
    bool SesionAbierta,
    DateTime? UltimaFechaApertura,
    /// <summary>false si la empresa no tiene fila en PARAMETROS_EMPRESA: no se puede abrir su caja.</summary>
    bool TieneParametros);
