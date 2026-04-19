using System.Diagnostics;
using Cbc.News.Api.Models;
using Cbc.News.Api.Services;

namespace Cbc.News.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, LogRepository logRepository)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        var logEntry = new LogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Level = context.Response.StatusCode >= 400 ? "Warning" : "Info",
            Service = "Cbc.News.Api",
            CorrelationId = correlationId,
            Path = context.Request.Path,
            Message = $"{context.Request.Method} {context.Request.Path}",
            StatusCode = context.Response.StatusCode,
            DurationMs = stopwatch.ElapsedMilliseconds
        };

        await logRepository.InsertAsync(logEntry);
    }
}