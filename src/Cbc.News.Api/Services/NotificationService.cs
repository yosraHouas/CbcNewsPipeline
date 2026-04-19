using Cbc.News.Api.Hubs;
using Cbc.News.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace Cbc.News.Api.Services;

public sealed class NotificationService
{
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly NotificationRepository _notificationRepository;

    public NotificationService(
        IHubContext<NotificationsHub> hubContext,
        NotificationRepository notificationRepository)
    {
        _hubContext = hubContext;
        _notificationRepository = notificationRepository;
    }
    public async Task NotifyIngestionRequestedAsync(NotificationPayload payload, CancellationToken ct = default)
    {
        var message = string.IsNullOrWhiteSpace(payload.FeedName)
            ? "Ingestion requested."
            : $"Ingestion requested for feed '{payload.FeedName}'.";

        await SaveAsync(
            type: "info",
            title: "Ingestion requested",
            message: message,
            feedName: payload.FeedName,
            ct);

        await _hubContext.Clients.All.SendAsync("ingestionRequested", payload, ct);
    }

    public async Task NotifyIngestionCompletedAsync(NotificationPayload payload, CancellationToken ct = default)
    {
        var message = !string.IsNullOrWhiteSpace(payload.FeedName) && payload.StoriesCount.HasValue
            ? $"Ingestion completed for feed '{payload.FeedName}' with {payload.StoriesCount.Value} stories."
            : "Ingestion completed successfully.";

        await SaveAsync(
            type: "success",
            title: "Ingestion complete",
            message: message,
            feedName: payload.FeedName,
            ct);

        await _hubContext.Clients.All.SendAsync("ingestionCompleted", payload, ct);
    }

    public async Task NotifyIngestionFailedAsync(NotificationPayload payload, CancellationToken ct = default)
    {
        var errorPart = string.IsNullOrWhiteSpace(payload.Error)
            ? ""
            : $" Error: {payload.Error}";

        var message = string.IsNullOrWhiteSpace(payload.FeedName)
            ? $"Ingestion failed.{errorPart}"
            : $"Ingestion failed for feed '{payload.FeedName}'.{errorPart}";

        await SaveAsync(
            type: "error",
            title: "Ingestion failed",
            message: message,
            feedName: payload.FeedName,
            ct);

        await _hubContext.Clients.All.SendAsync("ingestionFailed", payload, ct);
    }

    public Task<List<NotificationEvent>> GetLatestAsync(int limit = 50, CancellationToken ct = default)
    {
        return _notificationRepository.GetLatestAsync(limit, ct);
    }

    public Task<NotificationEvent?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return _notificationRepository.GetByIdAsync(id, ct);
    }

    private async Task SaveAsync(
        string type,
        string title,
        string message,
        string feedName,
        CancellationToken ct)
    {
        var notification = new NotificationEvent
        {
            Type = type,
            Title = title,
            Message = message,
            FeedName = feedName,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification, ct);
    }

    public async Task NotifyIngestionStartedAsync(NotificationPayload payload, CancellationToken ct = default)
    {
        var message = string.IsNullOrWhiteSpace(payload.FeedName)
            ? "Ingestion started."
            : $"Ingestion started for feed '{payload.FeedName}'.";

        await SaveAsync(
            type: "info",
            title: "Ingestion started",
            message: message,
            feedName: payload.FeedName,
            ct);

        await _hubContext.Clients.All.SendAsync("ingestionStarted", payload, ct);
    }
}