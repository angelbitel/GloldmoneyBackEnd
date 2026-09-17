namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;

public sealed class ParametrosEmpresaDb
{
    public string CodigoEmpresa { get; set; } = string.Empty;
    public decimal? PermitirDescuento { get; set; }
    public decimal? TrabajarKilates { get; set; }
    public decimal? ExistenciaReloj { get; set; }
    public decimal? BloquearPlazoInt { get; set; }
    public decimal? CodigoBarra { get; set; }
    public decimal? OperarAbonoParcial { get; set; }
    public decimal? AbonarVencidos { get; set; }
    public decimal? AbonarCapital { get; set; }
    public decimal? TipoCobroInteres { get; set; }
    public decimal? DiasGracia { get; set; }
    public decimal? ImprimirEmpActivos { get; set; }
    public decimal? ImprimirCopia { get; set; }
    public decimal? OperarEtiquetas { get; set; }
    public decimal? ControlarPuerto { get; set; }
    public string? PuertoContratos { get; set; }
    public string? PuertoPagos { get; set; }
    public decimal? TipoImpRecibo { get; set; }
    public decimal? ControlarCapital { get; set; }
    public decimal? TipoControlCapital { get; set; }
    public decimal? TipoValorCapital { get; set; }
    public decimal? ContMontoCaja { get; set; }
    public decimal? CostoCopia { get; set; }
    public decimal? AnulacionControladaTiempo { get; set; }
    public decimal? ImpEtiquetaCopia { get; set; }
    public decimal? DetallePrestablecido { get; set; }
    public decimal? RestarAbonoCapital { get; set; }
    public decimal? CapitalSinDecimal { get; set; }
    public decimal? PermitirPagoAdelantado { get; set; }
    public decimal? ImprimirReciboPago { get; set; }
    public decimal? ModeloImpresoraCodBar { get; set; }
    public decimal? ControlarAnulacion { get; set; }
    public decimal? ControlarImpresionContratos { get; set; }
    public int? ControlarProcesoAnulacion { get; set; }
    public int? NoEtiquetasPagos { get; set; }
    public int? MontoMinContrato { get; set; }
    public int? TipoEtiquetaPago { get; set; }
    public int? ImprimirEtiquetaRetiro { get; set; }
    public int? TiempoAnulacion { get; set; }
    public int? RepModuloSoporte { get; set; }
    public int? TipoMontoMaximo { get; set; }
    public decimal? PorcentajeMontoMaximo { get; set; }
    public int? ManejoCierreAutomatico { get; set; }
    public DateTime? HoraCierreAutomatico { get; set; }
    public DateTime? UltimoCierreAutomatico { get; set; }
    public int? StatusManejoScaner { get; set; }
    public string? RutaDirImagenes { get; set; }
    public int? MaxLengthCharDescripcion { get; set; }
    public int? MostrarComentarioCliente { get; set; }
    public int? MostrarComentarioContrato { get; set; }
    public int? MostrarColumnaCantProducto { get; set; }
    public int? NoEtiquetasRetiros { get; set; }
}

