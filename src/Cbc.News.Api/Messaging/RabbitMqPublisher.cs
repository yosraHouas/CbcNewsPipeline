using Cbc.News.Api.Options;
using Cbc.News.Contracts.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Cbc.News.Api.Messaging;

public sealed class RabbitMqPublisher : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IOptions<RabbitMqSettings> options)
    {
        var settings = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password
        };
    }

    private async Task EnsureInitializedAsync()
    {
        if (_connection is not null && _channel is not null)
            return;

        _connection = await _factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: RabbitMqQueues.IngestionRequests,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public async Task PublishAsync(object message)
    {
        await EnsureInitializedAsync();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await _channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: RabbitMqQueues.IngestionRequests,
            body: body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}