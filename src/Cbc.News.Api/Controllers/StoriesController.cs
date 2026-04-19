using Cbc.News.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbc.News.Api.Controllers;

[Authorize(Roles = "Admin,User")]
[ApiController]
[Route("stories")]
public class StoriesController : ControllerBase
{
    private readonly StoryRepository _storyRepository;

    public StoriesController(StoryRepository storyRepository)
    {
        _storyRepository = storyRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetLatest(
        [FromQuery] string? feed,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var totalCount = await _storyRepository.CountAsync(feed, ct);
        var items = await _storyRepository.GetLatestAsync(feed, limit, ct);

        return Ok(new
        {
            count = totalCount,
            feed,
            limit,
            items
        });
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount(
        [FromQuery] string? feed,
        CancellationToken ct = default)
    {
        var totalCount = await _storyRepository.CountAsync(feed, ct);

        return Ok(new
        {
            count = totalCount,
            feed
        });
    }
}