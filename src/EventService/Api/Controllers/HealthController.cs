using Microsoft.AspNetCore.Mvc;

namespace EventService.Api.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() =>
        Ok(new { status = "healthy", service = "EventService", timestamp = DateTime.UtcNow });
}
