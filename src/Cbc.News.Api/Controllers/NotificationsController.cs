using Cbc.News.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbc.News.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLatest(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var notifications = await _notificationService.GetLatestAsync(limit, ct);
        return Ok(notifications);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        string id,
        CancellationToken ct = default)
    {
        var notification = await _notificationService.GetByIdAsync(id, ct);

        if (notification is null)
            return NotFound(new { message = $"Notification '{id}' not found." });

        return Ok(notification);
    }
}