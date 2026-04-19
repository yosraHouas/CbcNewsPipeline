namespace Cbc.News.Contracts.Messaging;

public static class RabbitMqQueues
{
    public const string IngestionRequests = "cbc.ingestion.requests";
    public const string IngestionStarted = "cbc.ingestion.started";
    public const string IngestionCompleted = "cbc.ingestion.completed";
    public const string IngestionFailed = "cbc.ingestion.failed";

    // queue pour les logs API
    public const string ApiRequestLogs = "cbc.api.request.logs";
}