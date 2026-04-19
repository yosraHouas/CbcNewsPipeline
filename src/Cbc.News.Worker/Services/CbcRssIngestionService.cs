using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using Cbc.News.Worker.Models;

namespace Cbc.News.Worker.Services;

public class CbcRssIngestionService
{
    private readonly HttpClient _httpClient;
    private readonly IStoryRepository _repo;
    private readonly ILogger<CbcRssIngestionService> _logger;

    private static readonly Dictionary<string, (string feedKey, string url)> Feeds = new()
    {
        ["topstories"] = ("rss-topstories", "https://www.cbc.ca/webfeed/rss/rss-topstories"),
        ["technology"] = ("rss-technologys", "https://www.cbc.ca/webfeed/rss/rss-technology"),
        ["montreal"] = ("rss-canada-montreal", "https://www.cbc.ca/webfeed/rss/rss-canada-montreal"),
        ["politics"] = ("rss-politics", "https://www.cbc.ca/webfeed/rss/rss-politics"),
        ["business"] = ("rss-business", "https://www.cbc.ca/webfeed/rss/rss-business"),
        ["canada"] = ("rss-canada", "https://www.cbc.ca/webfeed/rss/rss-canada"),
        
    };

    public CbcRssIngestionService(
    HttpClient httpClient,
    IStoryRepository repo,
    ILogger<CbcRssIngestionService> logger)
    {
        _httpClient = httpClient;
        _repo = repo;
        _logger = logger;
    }

    public async Task<(int inserted, int updated)> IngestAsync(string feed, CancellationToken ct = default)
    {
        if (!Feeds.TryGetValue(feed.ToLowerInvariant(), out var f))
        {
            var supportedFeeds = string.Join(", ", Feeds.Keys.OrderBy(x => x));
            throw new ArgumentException($"Unknown feed '{feed}'. Supported feeds: {supportedFeeds}");
        }

        using var stream = await _httpClient.GetStreamAsync(f.url, ct);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });

        var syndication = SyndicationFeed.Load(reader);
        if (syndication is null)
            return (0, 0);

        var inserted = 0;
        var updated = 0;

        foreach (var item in syndication.Items)
        {
            ct.ThrowIfCancellationRequested();

            var link = item.Links?.FirstOrDefault()?.Uri?.ToString() ?? "";
            var externalId = item.Id;

            if (string.IsNullOrWhiteSpace(externalId))
                externalId = !string.IsNullOrWhiteSpace(link)
                    ? link
                    : Guid.NewGuid().ToString("N");

            var imageUrl = await TryGetImageUrlAsync(link, ct);

            var story = new Story
            {
                Source = "cbc-rss",
                Feed = feed.ToLowerInvariant(),
                ExternalId = externalId,
                Title = item.Title?.Text ?? "",
                Summary = item.Summary?.Text ?? "",
                Url = link,
                ImageUrl = imageUrl,
                PublishedAt = item.PublishDate != DateTimeOffset.MinValue
           ? item.PublishDate.UtcDateTime
           : DateTime.UtcNow,
                FetchedAt = DateTime.UtcNow
            };

            var (isInserted, isUpdated) = await _repo.UpsertAsync(story, ct);

            if (isInserted) inserted++;
            else if (isUpdated) updated++;
        }

        _logger.LogInformation(
            "RSS ingestion finished for feed {Feed}. Inserted={Inserted}, Updated={Updated}",
            feed, inserted, updated);

        return (inserted, updated);
    }

    private async Task<string> TryGetImageUrlAsync(string articleUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(articleUrl))
            return "";

        try
        {
            var html = await _httpClient.GetStringAsync(articleUrl, ct);

            var match = Regex.Match(
                html,
                "<meta\\s+property=[\"']og:image[\"']\\s+content=[\"'](?<url>[^\"']+)[\"']",
                RegexOptions.IgnoreCase);

            if (match.Success)
                return match.Groups["url"].Value;

            return "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to extract image from article {Url}", articleUrl);
            return "";
        }
    }
}