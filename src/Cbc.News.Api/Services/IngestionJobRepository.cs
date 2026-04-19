using Cbc.News.Api.Models;
using MongoDB.Driver;

namespace Cbc.News.Api.Services;

public sealed class IngestionJobRepository
{
    private readonly IMongoCollection<IngestionJob> _collection;

    public IngestionJobRepository(IMongoDatabase db)
    {
        _collection = db.GetCollection<IngestionJob>("ingestion_jobs");
    }

    public Task InsertAsync(IngestionJob job, CancellationToken ct = default) =>
        _collection.InsertOneAsync(job, cancellationToken: ct);

    public async Task<List<IngestionJob>> GetLatestAsync(int limit = 50, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        return await _collection
            .Find(_ => true)
            .SortByDescending(x => x.StartedAtUtc)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<IngestionJob?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task EnsureIndexesAsync()
    {
        var correlationIndex = new CreateIndexModel<IngestionJob>(
            Builders<IngestionJob>.IndexKeys.Ascending(x => x.CorrelationId),
            new CreateIndexOptions { Name = "ix_correlationId" });

        var startedAtIndex = new CreateIndexModel<IngestionJob>(
            Builders<IngestionJob>.IndexKeys.Descending(x => x.StartedAtUtc),
            new CreateIndexOptions { Name = "ix_startedAtUtc" });

        var statusIndex = new CreateIndexModel<IngestionJob>(
            Builders<IngestionJob>.IndexKeys.Ascending(x => x.Status),
            new CreateIndexOptions { Name = "ix_status" });

        await _collection.Indexes.CreateManyAsync(new[]
        {
            correlationIndex,
            startedAtIndex,
            statusIndex
        });
    }
}