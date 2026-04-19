namespace Cbc.News.Api.Models;

public sealed class LogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Info";
    public string Service { get; set; } = "Cbc.News.Api";
    public string? CorrelationId { get; set; }
    public string? Path { get; set; }
    public string Message { get; set; } = "";
    public string? Exception { get; set; }

    public int? StatusCode { get; set; }
    public long? DurationMs { get; set; }
}