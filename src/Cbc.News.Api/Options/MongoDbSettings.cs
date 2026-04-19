namespace Cbc.News.Api.Options;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "";
    public string Database { get; set; } = "cbcnews";
    public string Collection { get; set; } = "stories";
}