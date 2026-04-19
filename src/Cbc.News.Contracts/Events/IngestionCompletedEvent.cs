namespace Cbc.News.Contracts.Events;

public sealed class IngestionCompletedEvent
{
    public string JobId { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public string FeedName { get; set; } = "";
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}