namespace GoldmoneyBackend.Application.Pagos.Queries.GetConfiguracionCobro;

/// <summary>
/// Datos de la empresa y parámetros (PARAMETROS_EMPRESA) que la pantalla de cobro necesita para
/// calcular el retiro e imprimir el recibo y las etiquetas, como hace el VB6 tras cada cobro.
/// </summary>
/// <param name="RestarAbonoCapital">Si el interés pagado por adelantado se descuenta al retirar (<c>restar_abono_capital</c>).</param>
/// <param name="ImprimirReciboPago">Imprimir el comprobante de cada abono (<c>imprimir_recibo_pago</c>).</param>
/// <param name="ImprimirEtiquetas">La empresa trabaja con etiquetas (<c>operar_etiquetas</c>).</param>
/// <param name="EtiquetasPorPago">Etiquetas a imprimir por abono (<c>no_etiquetas_pagos</c>).</param>
/// <param name="ImprimirEtiquetaRetiro">Imprimir etiqueta al retirar (<c>imprimir_etiqueta_retiro</c>).</param>
/// <param name="EtiquetasPorRetiro">Etiquetas a imprimir por retiro (<c>no_etiquetas_retiros</c>).</param>
public sealed record ConfiguracionCobroDto(
    string CodigoEmpresa,
    string? NombreEmpresa,
    string? Direccion,
    string? Telefono,
    string? Ruc,
    bool RestarAbonoCapital,
    bool ImprimirReciboPago,
    bool ImprimirEtiquetas,
    int EtiquetasPorPago,
    bool ImprimirEtiquetaRetiro,
    int EtiquetasPorRetiro);
