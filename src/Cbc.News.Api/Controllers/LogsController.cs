using Cbc.News.Api.Middleware;
using Cbc.News.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbc.News.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("logs")]
public class LogsController : ControllerBase
{
    private readonly LogRepository _repo;

    public LogsController(LogRepository repo) => _repo = repo;

    [HttpGet("errors")]
    public async Task<IActionResult> GetErrors([FromQuery] int days = 7, [FromQuery] int limit = 50)
    {
        days = Math.Clamp(days, 1, 90);
        limit = Math.Clamp(limit, 1, 200);

        var sinceUtc = DateTime.UtcNow.AddDays(-days);
        var items = await _repo.GetErrorsAsync(sinceUtc, limit);

        return Ok(new
        {
            count = items.Count,
            days,
            limit,
            correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString(),
            items
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 200);

        var items = await _repo.GetAllAsync(limit);

        return Ok(new
        {
            count = items.Count,
            limit,
            correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString(),
            items
        });
    }
}