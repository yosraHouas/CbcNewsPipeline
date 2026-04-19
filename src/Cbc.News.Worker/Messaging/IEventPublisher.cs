namespace Cbc.News.Worker.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default);
}