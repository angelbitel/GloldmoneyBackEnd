using GoldmoneyBackend.Api.Contracts.Empenios;
using GoldmoneyBackend.Application.Empenios.Commands.CreateContrato;

namespace GoldmoneyBackend.Api.Mappings;

public static class EmpenioMappings
{
    public static CreateContratoCommand ToCommand(this CreateEmpenioContratoRequest request, string usuarioResponsable)
    {
        var detalles = (request.Detalles ?? [])
            .Select(d => new CreateContratoDetalleCommand(d.CodigoTipoPrenda, d.Descripcion, d.Peso, d.Kilataje, d.Cantidad))
            .ToList();

        return new CreateContratoCommand(
            request.CodigoEmpresa,
            request.CodigoGrupo,
            request.IdCliente,
            request.Nombre,
            request.Apellido,
            request.Direccion,
            request.Telefono,
            request.CodigoPais,
            request.CapitalPrestado,
            detalles,
            usuarioResponsable);
    }
}
