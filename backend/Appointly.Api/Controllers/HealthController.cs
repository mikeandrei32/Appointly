using Appointly.Api.Data;
using Appointly.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(AppointlyDbContext database, ILogger<HealthController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        try
        {
            // Query a migrated table: connecting alone does not prove the schema exists.
            await database.ClinicSchedules.CountAsync(cancellationToken);
            return Ok(new HealthResponse("healthy", "connected", "Asia/Manila"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Database health check failed.");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Database unavailable", detail: "Check database configuration and apply migrations.");
        }
    }
}
