namespace GoldmoneyBackend.Application.Common.Interfaces;

public sealed record EstadoSesionEmpresaDto(
    string CodigoEmpresa,
    string? NombreEmpresa,
    bool SesionAbierta,
    DateTime? UltimaFechaApertura);
