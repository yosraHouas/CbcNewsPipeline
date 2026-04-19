using System.Text;
using System.Text.Json;
using Cbc.News.Contracts.Events;
using Cbc.News.Contracts.Messaging;
using Cbc.News.Contracts.Model;
using Cbc.News.Worker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Cbc.News.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CbcRssIngestionService _ingestionService;
    private readonly IngestionJobRepository _jobRepository;
    private readonly EventRepository _eventRepository;

    public Worker(
        ILogger<Worker> logger,
        CbcRssIngestionService ingestionService,
        IngestionJobRepository jobRepository, EventRepository eventRepository)
    {
        _logger = logger;
        _ingestionService = ingestionService;
        _jobRepository = jobRepository;
        _eventRepository = eventRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost"
        };

        var connection = await factory.CreateConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: RabbitMqQueues.IngestionRequests,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: RabbitMqQueues.IngestionStarted,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: RabbitMqQueues.IngestionCompleted,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: RabbitMqQueues.IngestionFailed,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            _logger.LogInformation("📩 Message reçu : {Message}", json);

            const int maxRetries = 3;
            const int delayMs = 2000;

            string? jobId = null;
            string? correlationId = null;
            string feed = "montreal";

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("🔄 Tentative {Attempt}/{MaxRetries} de traitement du message", attempt, maxRetries);

                    var ingestionEvent = JsonSerializer.Deserialize<IngestionRequestedEvent>(json);

                    if (ingestionEvent is null || string.IsNullOrWhiteSpace(ingestionEvent.JobId))
                    {
                        _logger.LogInformation("ℹ️ Message ignoré : ce n'est pas une demande d'ingestion RSS valide.");

                        await channel.BasicAckAsync(
                            eventArgs.DeliveryTag,
                            multiple: false,
                            cancellationToken: stoppingToken);

                        return;
                    }

                    jobId = ingestionEvent.JobId;
                    correlationId = ingestionEvent.CorrelationId;

                    feed = string.IsNullOrWhiteSpace(ingestionEvent.FeedName)
                        ? "montreal"
                        : ingestionEvent.FeedName;

                    if (attempt == 1)
                    {
                        await _jobRepository.UpdateStatusAsync(
                            jobId,
                            status: "Running",
                            ct: stoppingToken);

                        _logger.LogInformation("📤 Publishing ingestion started event...");

                        await PublishEventAsync(
                            channel,
                            RabbitMqQueues.IngestionStarted,
                            new IngestionStartedEvent
                            {
                                JobId = jobId,
                                CorrelationId = correlationId ?? "",
                                FeedName = feed,
                                OccurredAtUtc = DateTime.UtcNow
                            },
                            stoppingToken);
                        await _eventRepository.AddAsync(new PipelineEvent
                        {
                            JobId = jobId,
                            EventType = "IngestionStarted",
                            FeedName = feed,
                            Message = $"Ingestion started for {feed}",
                            OccurredAtUtc = DateTime.UtcNow
                        });
                    }

                    _logger.LogInformation(
                        "🚀 Début ingestion RSS pour feed {Feed}, JobId={JobId}, CorrelationId={CorrelationId}",
                        feed,
                        jobId,
                        correlationId);

                    // => J’ai validé le retry en simulant une erreur au premier essai.
                    // Le Worker a retenté automatiquement et l’ingestion a finalement réussi.

                    //if (attempt == 1)
                    //{
                    //    throw new Exception("Erreur de test pour vérifier le retry");
                    //}

                    var (inserted, updated) = await _ingestionService.IngestAsync(feed, stoppingToken);

                    await _jobRepository.UpdateStatusAsync(
                        jobId,
                        status: "Completed",
                        storiesInserted: inserted,
                        storiesUpdated: updated,
                        finishedAtUtc: DateTime.UtcNow,
                        ct: stoppingToken);

                    _logger.LogInformation("📤 Publishing ingestion completed event...");

                    await PublishEventAsync(
                        channel,
                        RabbitMqQueues.IngestionCompleted,
                        new IngestionCompletedEvent
                        {
                            JobId = jobId,
                            CorrelationId = correlationId ?? "",
                            FeedName = feed,
                            InsertedCount = inserted,
                            UpdatedCount = updated,
                            OccurredAtUtc = DateTime.UtcNow
                        },
                        stoppingToken);
                    await _eventRepository.AddAsync(new PipelineEvent
                    {
                        JobId = jobId,
                        EventType = "IngestionCompleted",
                        FeedName = feed,
                        Message = $"Inserted={inserted}, Updated={updated}",
                        OccurredAtUtc = DateTime.UtcNow
                    });

                    _logger.LogInformation(
                        "✅ Ingestion terminée pour {Feed}. Inserted={Inserted}, Updated={Updated}, JobId={JobId}",
                        feed,
                        inserted,
                        updated,
                        jobId);

                    await channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Erreur pendant le traitement du message RabbitMQ - tentative {Attempt}/{MaxRetries}",
                        attempt,
                        maxRetries);

                    if (attempt == maxRetries)
                    {
                        if (!string.IsNullOrWhiteSpace(jobId))
                        {
                            await _jobRepository.UpdateStatusAsync(
                                jobId,
                                status: "Failed",
                                error: ex.Message,
                                finishedAtUtc: DateTime.UtcNow,
                                ct: stoppingToken);
                        }

                        await PublishEventAsync(
                            channel,
                            RabbitMqQueues.IngestionFailed,
                            new IngestionFailedEvent
                            {
                                JobId = jobId ?? "",
                                CorrelationId = correlationId ?? "",
                                FeedName = feed,
                                Error = ex.Message,
                                OccurredAtUtc = DateTime.UtcNow
                            },
                            stoppingToken);
                        await _eventRepository.AddAsync(new PipelineEvent
                        {
                            JobId = jobId ?? "",
                            EventType = "IngestionFailed",
                            FeedName = feed,
                            Message = ex.Message,
                            OccurredAtUtc = DateTime.UtcNow
                        });

                        _logger.LogWarning("🛑 Nombre maximal de tentatives atteint. Message rejeté.");

                        await channel.BasicNackAsync(
                            deliveryTag: eventArgs.DeliveryTag,
                            multiple: false,
                            requeue: false,
                            cancellationToken: stoppingToken);

                        return;
                    }

                    _logger.LogInformation("⏳ Nouvelle tentative dans {DelayMs} ms...", delayMs);
                    await Task.Delay(delayMs, stoppingToken);
                }
            }
        };

        await channel.BasicConsumeAsync(
            queue: RabbitMqQueues.IngestionRequests,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static async Task PublishEventAsync<T>(
        IChannel channel,
        string queueName,
        T message,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            body: body,
            cancellationToken: cancellationToken);
    }
}