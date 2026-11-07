using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Api.Contracts.Usuarios;
using GoldmoneyBackend.Application.Auth.Commands.ActualizarCorreoUsuario;
using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuariosRepository _usuariosRepository;
    private readonly ICuentasUsuarioService _cuentasUsuarioService;
    private readonly IMediator _mediator;

    public UsuariosController(
        IUsuariosRepository usuariosRepository,
        ICuentasUsuarioService cuentasUsuarioService,
        IMediator mediator)
    {
        _usuariosRepository = usuariosRepository;
        _cuentasUsuarioService = cuentasUsuarioService;
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var usuarios = await _usuariosRepository.GetAllAsync(cancellationToken);
        var correos = await _cuentasUsuarioService.GetCorreosAsync(cancellationToken);

        return Ok(usuarios.Select(u => u with { Correo = correos.GetValueOrDefault(u.NombreUsuario.Trim()) }));
    }

    /// <summary>
    /// Registra o cambia el correo del usuario (en todos sus módulos), usado para restablecer
    /// la contraseña. Un correo vacío lo elimina.
    /// </summary>
    [HttpPut("{nombreUsuario}/correo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActualizarCorreo(string nombreUsuario, [FromBody] ActualizarCorreoUsuarioRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActualizarCorreoUsuarioCommand(nombreUsuario, request.Correo), cancellationToken);
        return NoContent();
    }

    [HttpGet("{modulo}/{nombreUsuario}")]
    [ProducesResponseType(typeof(UsuarioDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string modulo, string nombreUsuario, CancellationToken cancellationToken)
    {
        var usuario = await _usuariosRepository.GetByKeyAsync(modulo, nombreUsuario, cancellationToken);
        if (usuario is null)
        {
            return NotFound();
        }

        return Ok(usuario);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDbDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioRequest request, CancellationToken cancellationToken)
    {
        var dto = new UsuarioDbUpsertDto(
            request.Modulo,
            request.NombreUsuario,
            request.NombreCompleto,
            request.Contrasena,
            request.StatusCuenta,
            request.IniciarDia,
            request.AplicarDescuento,
            request.UsuarioAdmin,
            request.DatosRetroactivos,
            request.EfectuarAnulacion,
            request.AccesoCashDrawer,
            request.UsuarioSoporte);

        await _usuariosRepository.CreateAsync(dto, cancellationToken);
        var created = await _usuariosRepository.GetByKeyAsync(request.Modulo, request.NombreUsuario, cancellationToken);
        return CreatedAtAction(nameof(GetByKey), new { modulo = request.Modulo, nombreUsuario = request.NombreUsuario }, created);
    }

    [HttpPut("{modulo}/{nombreUsuario}")]
    [ProducesResponseType(typeof(UsuarioDbDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string modulo, string nombreUsuario, [FromBody] UpdateUsuarioRequest request, CancellationToken cancellationToken)
    {
        var dto = new UsuarioDbUpsertDto(
            request.Modulo,
            request.NombreUsuario,
            request.NombreCompleto,
            request.Contrasena,
            request.StatusCuenta,
            request.IniciarDia,
            request.AplicarDescuento,
            request.UsuarioAdmin,
            request.DatosRetroactivos,
            request.EfectuarAnulacion,
            request.AccesoCashDrawer,
            request.UsuarioSoporte);

        await _usuariosRepository.UpdateAsync(modulo, nombreUsuario, dto, cancellationToken);
        var updated = await _usuariosRepository.GetByKeyAsync(request.Modulo, request.NombreUsuario, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{modulo}/{nombreUsuario}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string modulo, string nombreUsuario, CancellationToken cancellationToken)
    {
        await _usuariosRepository.DeleteAsync(modulo, nombreUsuario, cancellationToken);
        return NoContent();
    }
}
