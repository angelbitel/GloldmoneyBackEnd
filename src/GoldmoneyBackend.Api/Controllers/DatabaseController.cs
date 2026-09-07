using GoldmoneyBackend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldmoneyBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DatabaseController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpGet("connection")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnectionStatus(CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return Ok(new
        {
            connected = canConnect,
            server = connection.DataSource,
            database = connection.Database,
            provider = _dbContext.Database.ProviderName,
            state = canConnect ? "ok" : "not_connected"
        });
    }
}
