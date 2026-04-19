using Cbc.News.Api.Models;
using MongoDB.Driver;

namespace Cbc.News.Api.Services;

public class NotificationRepository
{
    private readonly IMongoCollection<NotificationEvent> _col;

    public NotificationRepository(IMongoDatabase db)
    {
        _col = db.GetCollection<NotificationEvent>("notification_events");
    }

    public async Task EnsureIndexesAsync()
    {
        await _col.Indexes.CreateOneAsync(
            new CreateIndexModel<NotificationEvent>(
                Builders<NotificationEvent>.IndexKeys.Descending(x => x.CreatedAtUtc),
                new CreateIndexOptions { Name = "ix_createdAtUtc_desc" }));
    }

    public async Task AddAsync(NotificationEvent notification, CancellationToken ct = default)
    {
        await _col.InsertOneAsync(notification, cancellationToken: ct);
    }

    public async Task<List<NotificationEvent>> GetLatestAsync(int limit = 50, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        return await _col.Find(Builders<NotificationEvent>.Filter.Empty)
            .SortByDescending(x => x.CreatedAtUtc)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<NotificationEvent?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _col.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
    }
}