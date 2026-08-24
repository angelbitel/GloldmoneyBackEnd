using GoldmoneyBackend.Api.Contracts.Empenios;
using GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

namespace GoldmoneyBackend.Api.Mappings;

public static class EmpenioMappings
{
    public static CreateContratoCommand ToCommand(this CreateEmpenioContratoRequest request)
    {
        var detalles = request.Detalles?
            .Select(d => new CreateContratoDetalleCommand(
                d.CodigoTipoPrenda,
                d.Descripcion,
                d.Peso,
                d.Kilataje,
                d.Cantidad,
                d.MontoAvaluo,
                d.MontoPrestamoDetalle,
                d.Observacion))
            .ToList();

        return new CreateContratoCommand(
            request.CodigoEmpresa,
            request.CodigoGrupo,
            request.NumeroContrato,
            request.IdCliente,
            request.Serie,
            request.FechaCreacion,
            request.CapitalPrestado,
            request.Interes,
            request.SaldoActual,
            request.Mensualidad,
            request.Observacion,
            request.UltimaFechaPago,
            request.SaldoCapital,
            request.FechaVencimiento,
            request.PlazoPago,
            request.Nombre,
            request.Apellido,
            request.Direccion,
            request.Telefono,
            request.MontoMaximo,
            request.UsuarioResponsable,
            request.CodigoPais,
            request.ProcesoKey,
            request.MontoMinimoEmpenio,
            request.ControlaCajaPorUsuario,
            request.ConfirmadoPorUsuario,
            request.TrabajoConKilates,
            request.ControlReloj,
            request.TipoTransaccion,
            detalles);
    }
}
