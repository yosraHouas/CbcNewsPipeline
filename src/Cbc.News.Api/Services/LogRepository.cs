using Cbc.News.Api.Models;
using MongoDB.Driver;

namespace Cbc.News.Api.Services;

public sealed class LogRepository
{
    private readonly IMongoCollection<LogEntry> _collection;

    public LogRepository(IMongoDatabase db)
    {
        _collection = db.GetCollection<LogEntry>("system_logs");
    }

    public Task InsertAsync(LogEntry entry) =>
        _collection.InsertOneAsync(entry);

    public async Task<List<LogEntry>> GetErrorsAsync(DateTime? sinceUtc, int limit = 50)
    {
        var filter = Builders<LogEntry>.Filter.Eq(x => x.Level, "Error");

        if (sinceUtc is not null)
        {
            filter &= Builders<LogEntry>.Filter.Gte(x => x.TimestampUtc, sinceUtc.Value);
        }

        return await _collection
            .Find(filter)
            .SortByDescending(x => x.TimestampUtc)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<List<LogEntry>> GetAllAsync(int limit = 50)
    {
        return await _collection
            .Find(_ => true)
            .SortByDescending(x => x.TimestampUtc)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task EnsureIndexesAsync()
    {
        var indexKeys = Builders<LogEntry>.IndexKeys
            .Descending(x => x.TimestampUtc);

        var indexModel = new CreateIndexModel<LogEntry>(indexKeys);

        await _collection.Indexes.CreateOneAsync(indexModel);
    }
}