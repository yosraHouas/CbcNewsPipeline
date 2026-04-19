using System.Text;
using System.Text.Json;
using Cbc.News.Api.Models;
using Cbc.News.Api.Services;
using Cbc.News.Contracts.Events;
using Cbc.News.Contracts.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cbc.News.Api.Messaging;

public sealed class IngestionEventsConsumer : BackgroundService
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<IngestionEventsConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public IngestionEventsConsumer(
        NotificationService notificationService,
        ILogger<IngestionEventsConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost"
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: RabbitMqQueues.IngestionStarted,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: RabbitMqQueues.IngestionCompleted,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: RabbitMqQueues.IngestionFailed,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var startedConsumer = new AsyncEventingBasicConsumer(_channel);
        startedConsumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var evt = JsonSerializer.Deserialize<IngestionStartedEvent>(json);

                if (evt is null)
                {
                    await _channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);
                    return;
                }

                await _notificationService.NotifyIngestionStartedAsync(
                    new NotificationPayload
                    {
                        FeedName = evt.FeedName,
                        JobId = evt.JobId,
                        CorrelationId = evt.CorrelationId,
                        Status = "Running",
                        StartedAtUtc = evt.OccurredAtUtc
                    },
                    stoppingToken);

                await _channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ingestion started event.");

                await _channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        var completedConsumer = new AsyncEventingBasicConsumer(_channel);
        completedConsumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var evt = JsonSerializer.Deserialize<IngestionCompletedEvent>(json);

                if (evt is null)
                {
                    await _channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);
                    return;
                }

                await _notificationService.NotifyIngestionCompletedAsync(
                    new NotificationPayload
                    {
                        FeedName = evt.FeedName,
                        JobId = evt.JobId,
                        CorrelationId = evt.CorrelationId,
                        Status = "Completed",
                        StoriesCount = evt.InsertedCount + evt.UpdatedCount
                    },
                    stoppingToken);

                await _channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ingestion completed event.");

                await _channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        var failedConsumer = new AsyncEventingBasicConsumer(_channel);
        failedConsumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                _logger.LogInformation("Received COMPLETED event: {Json}", json);
                var evt = JsonSerializer.Deserialize<IngestionFailedEvent>(json);

                if (evt is null)
                {
                    await _channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);
                    return;
                }

                await _notificationService.NotifyIngestionFailedAsync(
                    new NotificationPayload
                    {
                        FeedName = evt.FeedName,
                        JobId = evt.JobId,
                        CorrelationId = evt.CorrelationId,
                        Status = "Failed",
                        Error = evt.Error
                    },
                    stoppingToken);

                await _channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ingestion failed event.");

                await _channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: RabbitMqQueues.IngestionStarted,
            autoAck: false,
            consumer: startedConsumer,
            cancellationToken: stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: RabbitMqQueues.IngestionCompleted,
            autoAck: false,
            consumer: completedConsumer,
            cancellationToken: stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: RabbitMqQueues.IngestionFailed,
            autoAck: false,
            consumer: failedConsumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}