using System.Text.Json;

namespace Cbc.News.Worker.Messaging;

public class ConsoleEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);

        Console.WriteLine("EVENT PUBLISHED:");
        Console.WriteLine(json);

        return Task.CompletedTask;
    }
}