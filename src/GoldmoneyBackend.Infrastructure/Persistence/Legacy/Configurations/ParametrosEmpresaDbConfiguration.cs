using GoldmoneyBackend.Infrastructure.Persistence.Legacy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldmoneyBackend.Infrastructure.Persistence.Legacy.Configurations;

public sealed class ParametrosEmpresaDbConfiguration : IEntityTypeConfiguration<ParametrosEmpresaDb>
{
    public void Configure(EntityTypeBuilder<ParametrosEmpresaDb> builder)
    {
        builder.ToTable("PARAMETROS_EMPRESA");
        builder.HasKey(x => x.CodigoEmpresa);

        builder.Property(x => x.CodigoEmpresa)
            .HasColumnName("codigo_empresa")
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.PermitirDescuento).HasColumnName("permitir_descuento").HasColumnType("decimal(1,0)");
        builder.Property(x => x.TrabajarKilates).HasColumnName("trabajar_kilates").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ExistenciaReloj).HasColumnName("existencia_reloj").HasColumnType("decimal(1,0)");
        builder.Property(x => x.BloquearPlazoInt).HasColumnName("bloquear_plazo_int").HasColumnType("decimal(1,0)");
        builder.Property(x => x.CodigoBarra).HasColumnName("codigo_barra").HasColumnType("decimal(1,0)");
        builder.Property(x => x.OperarAbonoParcial).HasColumnName("operar_abono_parcial").HasColumnType("decimal(1,0)");
        builder.Property(x => x.AbonarVencidos).HasColumnName("abonar_vencidos").HasColumnType("decimal(1,0)");
        builder.Property(x => x.AbonarCapital).HasColumnName("abonar_capital").HasColumnType("decimal(1,0)");
        builder.Property(x => x.TipoCobroInteres).HasColumnName("tipo_cobro_interes").HasColumnType("decimal(1,0)");
        builder.Property(x => x.DiasGracia).HasColumnName("dias_gracia").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ImprimirEmpActivos).HasColumnName("imprimir_emp_activos").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ImprimirCopia).HasColumnName("imprimir_copia").HasColumnType("decimal(1,0)");
        builder.Property(x => x.OperarEtiquetas).HasColumnName("operar_etiquetas").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ControlarPuerto).HasColumnName("controlar_puerto").HasColumnType("decimal(1,0)");
        builder.Property(x => x.PuertoContratos).HasColumnName("puerto_contratos").HasMaxLength(30).IsUnicode(false);
        builder.Property(x => x.PuertoPagos).HasColumnName("puerto_pagos").HasMaxLength(30).IsUnicode(false);
        builder.Property(x => x.TipoImpRecibo).HasColumnName("tipo_imp_recibo").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ControlarCapital).HasColumnName("controlar_capital").HasColumnType("decimal(1,0)");
        builder.Property(x => x.TipoControlCapital).HasColumnName("tipo_control_capital").HasColumnType("decimal(1,0)");
        builder.Property(x => x.TipoValorCapital).HasColumnName("tipo_valor_capital").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ContMontoCaja).HasColumnName("cont_monto_caja").HasColumnType("decimal(1,0)");
        builder.Property(x => x.CostoCopia).HasColumnName("costo_copia").HasColumnType("decimal(10,2)");
        builder.Property(x => x.AnulacionControladaTiempo).HasColumnName("anulacion_controlada_tiempo").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ImpEtiquetaCopia).HasColumnName("imp_etiqueta_copia").HasColumnType("decimal(1,0)");
        builder.Property(x => x.DetallePrestablecido).HasColumnName("detalle_prestablecido").HasColumnType("decimal(1,0)");
        builder.Property(x => x.RestarAbonoCapital).HasColumnName("restar_abono_capital").HasColumnType("decimal(10,0)");
        builder.Property(x => x.CapitalSinDecimal).HasColumnName("capital_sin_decimal").HasColumnType("decimal(1,0)");
        builder.Property(x => x.PermitirPagoAdelantado).HasColumnName("permitir_pago_adelantado").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ImprimirReciboPago).HasColumnName("imprimir_recibo_pago").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ModeloImpresoraCodBar).HasColumnName("modelo_impresora_cod_bar").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ControlarAnulacion).HasColumnName("controlar_anulacion").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ControlarImpresionContratos).HasColumnName("controlar_impresion_contratos").HasColumnType("decimal(1,0)");
        builder.Property(x => x.ControlarProcesoAnulacion).HasColumnName("controlar_proceso_anulacion");
        builder.Property(x => x.NoEtiquetasPagos).HasColumnName("no_etiquetas_pagos");
        builder.Property(x => x.MontoMinContrato).HasColumnName("monto_min_contrato");
        builder.Property(x => x.TipoEtiquetaPago).HasColumnName("tipo_etiqueta_pago");
        builder.Property(x => x.ImprimirEtiquetaRetiro).HasColumnName("imprimir_etiqueta_retiro");
        builder.Property(x => x.TiempoAnulacion).HasColumnName("tiempo_anulacion");
        builder.Property(x => x.RepModuloSoporte).HasColumnName("rep_modulo_soporte");
        builder.Property(x => x.TipoMontoMaximo).HasColumnName("tipo_monto_maximo");
        builder.Property(x => x.PorcentajeMontoMaximo).HasColumnName("porcentaje_monto_maximo").HasColumnType("decimal(10,2)");
        builder.Property(x => x.ManejoCierreAutomatico).HasColumnName("manejo_cierre_automatico");
        builder.Property(x => x.HoraCierreAutomatico).HasColumnName("hora_cierre_automatico");
        builder.Property(x => x.UltimoCierreAutomatico).HasColumnName("ultimo_cierre_automatico");
        builder.Property(x => x.StatusManejoScaner).HasColumnName("status_manejo_scaner");
        builder.Property(x => x.RutaDirImagenes).HasColumnName("ruta_dir_imagenes").HasMaxLength(250).IsUnicode(false);
        builder.Property(x => x.MaxLengthCharDescripcion).HasColumnName("max_length_char_descripcion");
        builder.Property(x => x.MostrarComentarioCliente).HasColumnName("mostrar_comentario_cliente");
        builder.Property(x => x.MostrarComentarioContrato).HasColumnName("mostrar_comentario_contrato");
        builder.Property(x => x.MostrarColumnaCantProducto).HasColumnName("mostrar_columna_cant_producto");
        builder.Property(x => x.NoEtiquetasRetiros).HasColumnName("no_etiquetas_retiros");
    }
}
