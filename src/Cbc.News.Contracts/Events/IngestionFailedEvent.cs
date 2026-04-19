namespace Cbc.News.Contracts.Events;

public sealed class IngestionFailedEvent
{
    public string JobId { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public string FeedName { get; set; } = "";
    public string Error { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
}