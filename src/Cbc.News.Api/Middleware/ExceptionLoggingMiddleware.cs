using System.Diagnostics;
using Cbc.News.Api.Models;
using Cbc.News.Api.Services;

namespace Cbc.News.Api.Middleware;

public sealed class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, LogRepository repo)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

            var log = new LogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = "Error",
                Service = "Cbc.News.Api",
                CorrelationId = correlationId,
                Path = context.Request.Path,
                Message = ex.Message,
                Exception = ex.ToString(),
                StatusCode = 500,
                DurationMs = stopwatch.ElapsedMilliseconds
            };

            await repo.InsertAsync(log);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Internal server error",
                    correlationId
                });
            }
        }
    }
}