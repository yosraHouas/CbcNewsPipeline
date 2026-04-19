using Cbc.News.Worker.Models;
using MongoDB.Driver;

namespace Cbc.News.Worker.Services;

public sealed class IngestionJobRepository
{
    private readonly IMongoCollection<IngestionJob> _collection;

    public IngestionJobRepository(IMongoDatabase db)
    {
        _collection = db.GetCollection<IngestionJob>("ingestion_jobs");
    }

    public async Task UpdateStatusAsync(
        string jobId,
        string status,
        int storiesInserted = 0,
        int storiesUpdated = 0,
        string? error = null,
        DateTime? finishedAtUtc = null,
        CancellationToken ct = default)
    {
        var update = Builders<IngestionJob>.Update
            .Set(x => x.Status, status)
            .Set(x => x.StoriesInserted, storiesInserted)
            .Set(x => x.StoriesUpdated, storiesUpdated)
            .Set(x => x.Error, error);

        if (finishedAtUtc.HasValue)
        {
            update = update.Set(x => x.FinishedAtUtc, finishedAtUtc.Value);
        }

        await _collection.UpdateOneAsync(
            x => x.Id == jobId,
            update,
            cancellationToken: ct);
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