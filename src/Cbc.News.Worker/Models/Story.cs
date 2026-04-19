using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cbc.News.Worker.Models;

public class Story
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    public string Source { get; set; } = "cbc-rss";
    public string Feed { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Url { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}