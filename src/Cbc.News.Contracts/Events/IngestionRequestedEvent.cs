namespace Cbc.News.Contracts.Events;

public sealed class IngestionRequestedEvent
{
    public string JobId { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public string FeedName { get; set; } = "";
    public DateTime RequestedAtUtc { get; set; }
}