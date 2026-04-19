namespace Cbc.News.Api.Models;

public class NotificationPayload
{
    public string FeedName { get; set; } = "";
    public string JobId { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? StartedAtUtc { get; set; }
    public int? StoriesCount { get; set; }
    public string Error { get; set; } = "";
}