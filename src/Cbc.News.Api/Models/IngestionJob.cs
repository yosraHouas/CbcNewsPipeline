using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cbc.News.Api.Models;

public sealed class IngestionJob
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    public string CorrelationId { get; set; } = "";
    public string Feed { get; set; } = "";
    public string Status { get; set; } = "Pending";

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }

    public int StoriesInserted { get; set; }
    public int StoriesUpdated { get; set; }

    public string? Error { get; set; }
}