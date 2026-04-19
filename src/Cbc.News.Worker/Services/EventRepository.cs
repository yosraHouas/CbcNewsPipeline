using Cbc.News.Contracts.Model;
using MongoDB.Driver;


namespace Cbc.News.Worker.Services
{
    public class EventRepository
    {
        private readonly IMongoCollection<PipelineEvent> _collection;

        public EventRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<PipelineEvent>("pipelineEvents");
        }

        public Task AddAsync(PipelineEvent evt)
        {
            return _collection.InsertOneAsync(evt);
        }
    }
}
