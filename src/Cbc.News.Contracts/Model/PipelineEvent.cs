namespace Cbc.News.Contracts.Model
{
    public class PipelineEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string JobId { get; set; } = default!;
        public string EventType { get; set; } = default!;
        public string FeedName { get; set; } = default!;
        public string Message { get; set; } = default!;
        public DateTime OccurredAtUtc { get; set; }
    }
}
