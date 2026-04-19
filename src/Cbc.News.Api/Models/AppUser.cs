using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cbc.News.Api.Models;

public class AppUser
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
}