using Cbc.News.Worker.Models;

namespace Cbc.News.Worker.Services;

public interface IStoryRepository
{
    Task<(bool inserted, bool updated)> UpsertAsync(Story s, CancellationToken ct = default);
    Task<List<Story>> GetLatestAsync(string? feed, int limit);
    Task EnsureIndexesAsync();
}