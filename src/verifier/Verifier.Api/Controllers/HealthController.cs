using Microsoft.AspNetCore.Mvc;

namespace Verifier.Api.Controllers;

[ApiController]
[Route("health")]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Comprueba que el servicio está operativo (liveness check).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "healthy", service = "verifier" });
}
