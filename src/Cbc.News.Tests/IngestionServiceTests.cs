using Cbc.News.Worker.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using Cbc.News.Worker.Models;

namespace Cbc.News.Tests;

public class IngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_Should_Throw_When_Feed_Is_Unknown()
    {
        var repo = new Mock<IStoryRepository>();
        var logger = new Mock<ILogger<CbcRssIngestionService>>();
        var httpClient = new HttpClient();

        var service = new CbcRssIngestionService(
            httpClient,
            repo.Object,
            logger.Object);

        var act = async () => await service.IngestAsync("invalid-feed");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unknown feed*");
    }

    [Fact]
    public async Task IngestAsync_Should_Upsert_Stories_From_Rss_Feed()
    {
        var rssXml = """
        <?xml version="1.0" encoding="UTF-8" ?>
        <rss version="2.0">
          <channel>
            <title>CBC Test Feed</title>
            <item>
              <guid>story-1</guid>
              <title>Story One</title>
              <description>Summary one</description>
              <link>https://example.com/story-1</link>
              <pubDate>2026-03-18T10:00:00Z</pubDate>
            </item>
            <item>
              <guid>story-2</guid>
              <title>Story Two</title>
              <description>Summary two</description>
              <link>https://example.com/story-2</link>
              <pubDate>2026-03-18T11:00:00Z</pubDate>
            </item>
          </channel>
        </rss>
        """;

        var articleHtml = """
        <html>
          <head>
            <meta property="og:image" content="https://example.com/image.jpg" />
          </head>
          <body>test</body>
        </html>
        """;

        var handler = new FakeHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();

            if (url.Contains("rss-canada-montreal"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(rssXml, Encoding.UTF8, "application/xml")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(articleHtml, Encoding.UTF8, "text/html")
            };
        });

        var httpClient = new HttpClient(handler);

        var repo = new Mock<IStoryRepository>();
        repo.Setup(x => x.UpsertAsync(It.IsAny<Story>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, false));

        var logger = new Mock<ILogger<CbcRssIngestionService>>();

        var service = new CbcRssIngestionService(
            httpClient,
            repo.Object,
            logger.Object);

        var result = await service.IngestAsync("montreal");

        result.inserted.Should().Be(2);
        result.updated.Should().Be(0);

        repo.Verify(x => x.UpsertAsync(
            It.IsAny<Story>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task IngestAsync_Should_Return_Updated_Count_When_Repository_Reports_Updates()
    {
        var rssXml = """
        <?xml version="1.0" encoding="UTF-8" ?>
        <rss version="2.0">
          <channel>
            <title>CBC Test Feed</title>
            <item>
              <guid>story-1</guid>
              <title>Story One</title>
              <description>Summary one</description>
              <link>https://example.com/story-1</link>
              <pubDate>2026-03-18T10:00:00Z</pubDate>
            </item>
          </channel>
        </rss>
        """;

        var articleHtml = """
        <html>
          <head>
            <meta property="og:image" content="https://example.com/image.jpg" />
          </head>
          <body>test</body>
        </html>
        """;

        var handler = new FakeHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();

            if (url.Contains("rss-canada-montreal"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(rssXml, Encoding.UTF8, "application/xml")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(articleHtml, Encoding.UTF8, "text/html")
            };
        });

        var httpClient = new HttpClient(handler);

        var repo = new Mock<IStoryRepository>();
        repo.Setup(x => x.UpsertAsync(It.IsAny<Story>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, true));

        var logger = new Mock<ILogger<CbcRssIngestionService>>();

        var service = new CbcRssIngestionService(
            httpClient,
            repo.Object,
            logger.Object);

        var result = await service.IngestAsync("montreal");

        result.inserted.Should().Be(0);
        result.updated.Should().Be(1);

        repo.Verify(x => x.UpsertAsync(
            It.IsAny<Story>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}