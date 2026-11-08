namespace GoldmoneyBackend.Infrastructure.Persistence.Cobros;

/// <summary>
/// Un cobro de la pantalla de pagos: cómo pagó el cliente y qué recibos cubre. El VB6 no
/// registra la forma de pago, así que va en tablas propias del sistema nuevo.
/// </summary>
public sealed class Cobro
{
    public Guid Id { get; set; }
    public DateTime FechaUtc { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string IdCliente { get; set; } = string.Empty;
    public decimal TotalMetodos { get; set; }
    public decimal TotalDocumentos { get; set; }
    public List<CobroMetodoPago> Metodos { get; set; } = [];
    public List<CobroDocumento> Documentos { get; set; } = [];
}

public sealed class CobroMetodoPago
{
    public int Id { get; set; }
    public Guid CobroId { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}

/// <summary>Recibo de abono o retiro cubierto por el cobro.</summary>
public sealed class CobroDocumento
{
    public int Id { get; set; }
    public Guid CobroId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string CodigoEmpresa { get; set; } = string.Empty;
    public int CodigoGrupo { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public int NumeroRecibo { get; set; }
    public decimal Total { get; set; }
}
