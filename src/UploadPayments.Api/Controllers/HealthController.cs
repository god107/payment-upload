using Microsoft.AspNetCore.Mvc;
using UploadPayments.Infrastructure.Persistence;

namespace UploadPayments.Api.Controllers;

[ApiController]
public sealed class HealthController(UploadPaymentsDbContext db) : ControllerBase
{
    [HttpGet("/health")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        
        if (canConnect)
        {
            return Ok(new { status = "ok" });
        }
        
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "db_unavailable" });
    }
}
