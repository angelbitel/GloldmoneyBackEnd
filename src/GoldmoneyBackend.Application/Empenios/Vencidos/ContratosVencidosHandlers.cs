using GoldmoneyBackend.Application.Common.Interfaces;
using GoldmoneyBackend.Application.Empenios.Transacciones;
using GoldmoneyBackend.Domain.Common;
using MediatR;

namespace GoldmoneyBackend.Application.Empenios.Vencidos;

/// <summary>Contratos vencidos/retirados filtrados, con búsqueda libre y paginados en el servidor.</summary>
public sealed record BuscarContratosVencidosQuery(
    FiltroContratosVencidos Filtro,
    string? Buscar = null,
    int Pagina = 1,
    int TamanoPagina = BusquedaVencidos.TamanoPaginaPorDefecto) : IRequest<ContratosVencidosResultDto>;

/// <summary>Claves de todos los contratos del resultado (para "seleccionar todos").</summary>
public sealed record ClavesContratosVencidosQuery(FiltroContratosVencidos Filtro, string? Buscar = null)
    : IRequest<IReadOnlyList<ContratoVencidoClaveDto>>;

public sealed record DetalleContratoVencidoQuery(ContratoClave Contrato, ModoVencidos Modo) : IRequest<DetalleContratoVencidoDto>;

public sealed record RetirarContratosVencidosCommand(IReadOnlyList<ContratoClave> Contratos, string UsuarioResponsable)
    : IRequest<ProcesoVencidosResultDto>;

public sealed record DeshacerRetiroContratosCommand(IReadOnlyList<ContratoClave> Contratos) : IRequest<ProcesoVencidosResultDto>;

internal static class ContratosVencidosMapper
{
    public static ContratoVencidoDto ToDto(ContratoVencidoDatos c, ContextoVencimiento contexto)
    {
        var mesesPagados = ReglasVencimiento.MesesPagados(c.FechaCreacion, c.UltimaFechaPago);
        var mesesDebe = ReglasVencimiento.MesesQueDebe(c.FechaCreacion, c.UltimaFechaPago, contexto.FechaSistema, contexto.DiasGracia);
        var saldoCapital = c.SaldoCapital ?? 0m;
        var numero = c.NumeroContrato.Trim();
        var serie = string.IsNullOrWhiteSpace(c.Serie) ? SerieDe(numero) : c.Serie.Trim();
        var cliente = string.Join(' ', new[] { c.Nombre?.Trim(), c.Apellido?.Trim() }.Where(x => !string.IsNullOrEmpty(x)));

        return new ContratoVencidoDto(
            c.CodigoEmpresa.Trim(), c.CodigoGrupo, c.AbreviaturaGrupo?.Trim(), c.NumeroContrato.Trim(), serie,
            c.IdCliente?.Trim(), cliente, mesesPagados, mesesDebe, c.PlazoPago,
            c.CapitalPrestado ?? 0m, saldoCapital, (c.SaldoActual ?? 0m) - saldoCapital,
            c.FechaCreacion, c.FechaVencimiento, c.UltimaFechaPago, c.Observacion?.Trim(), c.FechaRetiro);
    }

    /// <summary>Como SADE: la serie son los últimos 10 dígitos del número de contrato.</summary>
    public static string SerieDe(string numeroContrato)
    {
        var numero = numeroContrato.Trim();
        return numero.Length > 10 ? numero[^10..] : numero;
    }

    public static IReadOnlyList<ContratoClave> Normalizar(IReadOnlyList<ContratoClave>? contratos)
    {
        var claves = (contratos ?? [])
            .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.CodigoEmpresa) && !string.IsNullOrWhiteSpace(x.NumeroContrato))
            .Select(x => x.Normalizada())
            .Distinct()
            .ToList();

        if (claves.Count == 0)
            throw new DomainValidationException("Estimado usuario, no ha seleccionado contratos para efectuar el proceso.");

        if (claves.Count > ReglasVencimiento.MaximoContratosPorProceso)
            throw new DomainValidationException(
                $"Estimado usuario, solo se pueden procesar {ReglasVencimiento.MaximoContratosPorProceso} contratos a la vez.");

        return claves;
    }
}

/// <summary>Equivale a Localizar_Contratos_Seleccionados (sp_Contratos_Vencidos_de_la_Empresa / sp_Contratos_Retirados_por_la_Empresa).</summary>
public sealed class BuscarContratosVencidosQueryHandler :
    IRequestHandler<BuscarContratosVencidosQuery, ContratosVencidosResultDto>,
    IRequestHandler<ClavesContratosVencidosQuery, IReadOnlyList<ContratoVencidoClaveDto>>
{
    private readonly IContratosVencidosRepository _repository;

    public BuscarContratosVencidosQueryHandler(IContratosVencidosRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContratosVencidosResultDto> Handle(BuscarContratosVencidosQuery request, CancellationToken cancellationToken)
    {
        var (filtro, contexto, datos, contratos) = await ObtenerAsync(request.Filtro, request.Buscar, cancellationToken);
        var (pagina, tamano) = BusquedaVencidos.NormalizarPagina(request.Pagina, request.TamanoPagina);

        // Totales y grupos sobre todo el resultado; solo la página viaja al cliente.
        var grupos = contratos
            .GroupBy(x => x.CodigoGrupo)
            .Select(g =>
            {
                var descripcion = datos.FirstOrDefault(d => d.CodigoGrupo == g.Key)?.DescripcionGrupo?.Trim();
                return Totalizar(g.Key, g.First().AbreviaturaGrupo, descripcion, g.ToList());
            })
            .ToList();

        return new ContratosVencidosResultDto(
            filtro.Modo, contexto.FechaSistema, contexto.DiasGracia, contratos.Count, false,
            Totalizar(null, null, null, contratos), grupos,
            BusquedaVencidos.Paginar(contratos, pagina, tamano), pagina, tamano);
    }

    public async Task<IReadOnlyList<ContratoVencidoClaveDto>> Handle(ClavesContratosVencidosQuery request, CancellationToken cancellationToken)
    {
        var (_, _, _, contratos) = await ObtenerAsync(request.Filtro, request.Buscar, cancellationToken);
        return contratos
            .Select(c => new ContratoVencidoClaveDto(c.CodigoEmpresa, c.CodigoGrupo, c.NumeroContrato, c.Serie, c.CapitalPrestado))
            .ToList();
    }

    private async Task<(FiltroContratosVencidos Filtro, ContextoVencimiento Contexto, IReadOnlyList<ContratoVencidoDatos> Datos, List<ContratoVencidoDto> Contratos)>
        ObtenerAsync(FiltroContratosVencidos filtroOriginal, string? buscar, CancellationToken cancellationToken)
    {
        var filtro = ReglasVencimiento.Validar(filtroOriginal);
        var contexto = await _repository.GetContextoAsync(filtro.CodigoEmpresa, cancellationToken);
        var datos = await _repository.GetContratosAsync(filtro, cancellationToken);

        var contratos = datos
            .Select(c => ContratosVencidosMapper.ToDto(c, contexto))
            // En retirados no se exige seguir vencido: cualquier contrato retirado por la casa se puede restablecer.
            .Where(x => filtro.Modo == ModoVencidos.Retirados || ReglasVencimiento.EstaVencido(x.MesesDebe, x.PlazoPago))
            .Where(x => ReglasVencimiento.CumpleFiltro(filtro, x.MesesPagados, x.MesesDebe))
            .OrderBy(x => x.CodigoGrupo)
            .ThenBy(x => x.NumeroContrato)
            .ToList();

        return (filtro, contexto, datos, BusquedaVencidos.Filtrar(contratos, buscar));
    }

    private static TotalesVencidosDto Totalizar(int? grupo, string? abreviatura, string? descripcion, IReadOnlyCollection<ContratoVencidoDto> contratos) =>
        new(grupo, abreviatura, descripcion, contratos.Count,
            contratos.Sum(x => x.CapitalPrestado), contratos.Sum(x => x.SaldoCapital), contratos.Sum(x => x.SaldoInteres));
}

/// <summary>Equivale a jgxContratos_RowColChange + Desplegar_Numero_pagos.</summary>
public sealed class DetalleContratoVencidoQueryHandler : IRequestHandler<DetalleContratoVencidoQuery, DetalleContratoVencidoDto>
{
    private readonly IContratosVencidosRepository _repository;

    public DetalleContratoVencidoQueryHandler(IContratosVencidosRepository repository)
    {
        _repository = repository;
    }

    public async Task<DetalleContratoVencidoDto> Handle(DetalleContratoVencidoQuery request, CancellationToken cancellationToken)
    {
        var clave = request.Contrato.Normalizada();
        var contrato = await _repository.GetContratoAsync(clave, request.Modo, cancellationToken)
            ?? throw new NotFoundDomainException("Estimado usuario, el número de contrato no existe en la base de datos.");

        var contexto = await _repository.GetContextoAsync(clave.CodigoEmpresa, cancellationToken);
        var detalle = await _repository.GetDetalleAsync(clave, request.Modo, cancellationToken);
        var dto = ContratosVencidosMapper.ToDto(contrato, contexto);

        return new DetalleContratoVencidoDto(
            dto,
            detalle.Prendas,
            detalle.Pagos,
            detalle.MostrarComentarioContrato && !string.IsNullOrWhiteSpace(dto.Observacion) ? dto.Observacion : null,
            detalle.MostrarComentarioCliente && !string.IsNullOrWhiteSpace(detalle.ComentarioCliente) ? detalle.ComentarioCliente.Trim() : null);
    }
}

/// <summary>Salvar_Proceso_Seleccionado (retiro): el contrato pasa al historial con status CF.</summary>
public sealed class RetirarContratosVencidosCommandHandler : IRequestHandler<RetirarContratosVencidosCommand, ProcesoVencidosResultDto>
{
    private readonly IContratosVencidosRepository _repository;
    private readonly ITransaccionesContratoRepository _transacciones;

    public RetirarContratosVencidosCommandHandler(IContratosVencidosRepository repository, ITransaccionesContratoRepository transacciones)
    {
        _repository = repository;
        _transacciones = transacciones;
    }

    public async Task<ProcesoVencidosResultDto> Handle(RetirarContratosVencidosCommand request, CancellationToken cancellationToken)
    {
        var claves = ContratosVencidosMapper.Normalizar(request.Contratos);
        var usuario = Usuario.Normalizar(request.UsuarioResponsable);
        var contextos = new Dictionary<string, ContextoVencimiento>();
        var capital = 0m;
        var series = new List<string>();

        foreach (var clave in claves)
        {
            var contrato = await _repository.GetContratoAsync(clave, ModoVencidos.Vencidos, cancellationToken)
                ?? throw new NotFoundDomainException($"Estimado usuario, el contrato {clave.NumeroContrato} ya no se encuentra activo.");

            if (!contextos.TryGetValue(clave.CodigoEmpresa, out var contexto))
                contextos[clave.CodigoEmpresa] = contexto = await _repository.GetContextoAsync(clave.CodigoEmpresa, cancellationToken);

            var dto = ContratosVencidosMapper.ToDto(contrato, contexto);
            if (!ReglasVencimiento.EstaVencido(dto.MesesDebe, dto.PlazoPago))
                throw new DomainValidationException($"Estimado usuario, el contrato {dto.Serie} no se encuentra vencido, por lo que no es posible retirarlo.");

            capital += dto.CapitalPrestado;
            series.Add(dto.Serie);
        }

        // La fecha de ingreso al historial es la fecha de trabajo de la caja (sesión abierta) o, sin sesión, la del servidor.
        var ahora = await _transacciones.GetFechaServidorAsync(cancellationToken);
        var sesion = await _transacciones.GetSesionOperacionAsync(claves[0].CodigoEmpresa, cancellationToken);
        var fecha = sesion?.FechaApertura.Date ?? ahora.Date;

        await _repository.RetirarAsync(new RetiroContratosDatos(claves, usuario, fecha, ahora), cancellationToken);
        return new ProcesoVencidosResultDto(claves.Count, capital, series);
    }
}

/// <summary>Salvar_Proceso_Seleccionado (deshacer): sp_ad_Restablecer_Contrato_Retirado con status CF.</summary>
public sealed class DeshacerRetiroContratosCommandHandler : IRequestHandler<DeshacerRetiroContratosCommand, ProcesoVencidosResultDto>
{
    private readonly IContratosVencidosRepository _repository;

    public DeshacerRetiroContratosCommandHandler(IContratosVencidosRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProcesoVencidosResultDto> Handle(DeshacerRetiroContratosCommand request, CancellationToken cancellationToken)
    {
        var claves = ContratosVencidosMapper.Normalizar(request.Contratos);
        var capital = 0m;
        var series = new List<string>();

        foreach (var clave in claves)
        {
            var contrato = await _repository.GetContratoAsync(clave, ModoVencidos.Retirados, cancellationToken)
                ?? throw new NotFoundDomainException($"Estimado usuario, el contrato {clave.NumeroContrato} no se encuentra entre los contratos retirados por la casa.");

            capital += contrato.CapitalPrestado ?? 0m;
            series.Add(ContratosVencidosMapper.SerieDe(contrato.NumeroContrato));
        }

        await _repository.DeshacerRetiroAsync(claves, cancellationToken);
        return new ProcesoVencidosResultDto(claves.Count, capital, series);
    }
}
