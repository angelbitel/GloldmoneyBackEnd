using GoldmoneyBackend.Api.Authorization;
using GoldmoneyBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/categorias-prenda")]
[Authorize(Policy = AuthorizationPolicies.Backoffice)]
public sealed class CategoriasPrendaController : ControllerBase
{
    private readonly ICategoriasPrendaRepository _categoriasPrendaRepository;

    public CategoriasPrendaController(ICategoriasPrendaRepository categoriasPrendaRepository)
    {
        _categoriasPrendaRepository = categoriasPrendaRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaPrendaDbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _categoriasPrendaRepository.GetAllAsync(cancellationToken));
    }

    [HttpGet("{codigoCategoriaPrenda:int}")]
    [ProducesResponseType(typeof(CategoriaPrendaDbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int codigoCategoriaPrenda, CancellationToken cancellationToken)
    {
        var categoria = await _categoriasPrendaRepository.GetByIdAsync(codigoCategoriaPrenda, cancellationToken);
        return categoria is null ? NotFound() : Ok(categoria);
    }
}
