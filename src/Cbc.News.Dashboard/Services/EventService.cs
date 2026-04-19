using Cbc.News.Contracts.Model;
using MongoDB.Driver;

namespace Cbc.News.Dashboard.Services;

public class EventService
{
    private readonly IMongoCollection<PipelineEvent> _collection;

    public EventService(IMongoDatabase database)
    {
        _collection = database.GetCollection<PipelineEvent>("pipelineEvents");
    }

    public async Task<List<PipelineEvent>> GetLatestAsync()
    {
        return await _collection
            .Find(_ => true)
            .SortByDescending(x => x.OccurredAtUtc)
            .Limit(50)
            .ToListAsync();
    }
}