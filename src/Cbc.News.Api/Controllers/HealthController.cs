using Microsoft.AspNetCore.Mvc;

namespace Cbc.News.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "OK",
            service = "Cbc.News.Api",
            timestamp = DateTime.UtcNow
        });
    }
}