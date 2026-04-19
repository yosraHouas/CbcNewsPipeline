using Cbc.News.Dashboard.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Cbc.News.Dashboard.Services;

public class IngestionNotifier
{
    private readonly IHubContext<IngestionHub> _hub;

    public IngestionNotifier(IHubContext<IngestionHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyStart(string feed)
    {
        await _hub.Clients.All.SendAsync("IngestionEvent", new
        {
            type = "info",
            title = "Ingestion started",
            message = $"Feed '{feed}' started."
        });
    }

    public async Task NotifySuccess(string feed, int inserted, int updated)
    {
        await _hub.Clients.All.SendAsync("IngestionEvent", new
        {
            type = "success",
            title = "Ingestion complete",
            message = $"{inserted} inserted, {updated} updated for {feed}"
        });
    }

    public async Task NotifyError(string feed, string error)
    {
        await _hub.Clients.All.SendAsync("IngestionEvent", new
        {
            type = "error",
            title = "Ingestion failed",
            message = $"{feed} failed: {error}"
        });
    }
}