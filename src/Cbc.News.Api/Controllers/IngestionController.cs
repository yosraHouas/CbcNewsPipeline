using Cbc.News.Contracts.Events;
using Cbc.News.Api.Models;
using Cbc.News.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Cbc.News.Api.Messaging;
using Microsoft.AspNetCore.Authorization;

namespace Cbc.News.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("ingestions")]
public class IngestionController : ControllerBase
{
    private readonly RabbitMqPublisher _publisher;
    private readonly IngestionJobRepository _jobRepository;
    private readonly NotificationService _notificationService;

    public IngestionController(
        RabbitMqPublisher publisher,
        IngestionJobRepository jobRepository,
        NotificationService notificationService)
    {
        _publisher = publisher;
        _jobRepository = jobRepository;
        _notificationService = notificationService;
    }

    [HttpPost("rss")]
    public async Task<IActionResult> RequestRssIngestion(
        [FromQuery] string feed = "montreal",
        CancellationToken ct = default)
    {
        var correlationId =
            HttpContext.Items[Middleware.CorrelationIdMiddleware.HeaderName]?.ToString()
            ?? HttpContext.TraceIdentifier;

        var job = new IngestionJob
        {
            CorrelationId = correlationId,
            Feed = feed,
            Status = "Pending",
            StartedAtUtc = DateTime.UtcNow
        };

        await _jobRepository.InsertAsync(job, ct);

        await _notificationService.NotifyIngestionRequestedAsync(
            new NotificationPayload
            {
                FeedName = feed,
                JobId = job.Id ?? "",
                CorrelationId = correlationId,
                Status = job.Status,
                StartedAtUtc = job.StartedAtUtc
            },
            ct);

        var message = new IngestionRequestedEvent
        {
            JobId = job.Id ?? "",
            CorrelationId = correlationId,
            FeedName = feed,
            RequestedAtUtc = DateTime.UtcNow
        };

        await _publisher.PublishAsync(message);

        return Accepted(new
        {
            jobId = job.Id,
            correlationId,
            feed,
            status = job.Status
        });
    }
}