using Cbc.News.Worker.Models;
using Cbc.News.Worker.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;


namespace Cbc.News.Worker.Services;

public class StoryRepository : IStoryRepository
{
    private readonly IMongoCollection<Story> _col;

    public StoryRepository(IMongoDatabase db, IOptions<MongoDbSettings> opts)
    {
        _col = db.GetCollection<Story>(opts.Value.Collection);
    }

    public async Task EnsureIndexesAsync()
    {
        var uq = Builders<Story>.IndexKeys
            .Ascending(x => x.Source)
            .Ascending(x => x.ExternalId);

        await _col.Indexes.CreateOneAsync(new CreateIndexModel<Story>(
            uq,
            new CreateIndexOptions { Unique = true, Name = "uq_source_externalId" }));

        await _col.Indexes.CreateOneAsync(new CreateIndexModel<Story>(
            Builders<Story>.IndexKeys.Descending(x => x.PublishedAt),
            new CreateIndexOptions { Name = "ix_publishedAt" }));
    }

    public async Task<(bool inserted, bool updated)> UpsertAsync(Story s, CancellationToken ct = default)
    {
        var filter = Builders<Story>.Filter.Eq(x => x.Source, s.Source) &
                     Builders<Story>.Filter.Eq(x => x.ExternalId, s.ExternalId);

        var update = Builders<Story>.Update
            .Set(x => x.Feed, s.Feed)
            .Set(x => x.Title, s.Title)
            .Set(x => x.Summary, s.Summary)
            .Set(x => x.Url, s.Url)
            .Set(x => x.ImageUrl, s.ImageUrl)
            .Set(x => x.PublishedAt, s.PublishedAt)
            .Set(x => x.FetchedAt, s.FetchedAt);

        var result = await _col.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);

        var inserted = result.UpsertedId != null;
        var updated = !inserted && result.ModifiedCount > 0;

        return (inserted, updated);
    }

    public async Task<List<Story>> GetLatestAsync(string? feed, int limit)
    {
        limit = Math.Clamp(limit, 1, 50);

        var filter = string.IsNullOrWhiteSpace(feed)
            ? Builders<Story>.Filter.Empty
            : Builders<Story>.Filter.Eq(x => x.Feed, feed);

        return await _col.Find(filter)
            .SortByDescending(x => x.PublishedAt)
            .Limit(limit)
            .ToListAsync();
    }
}