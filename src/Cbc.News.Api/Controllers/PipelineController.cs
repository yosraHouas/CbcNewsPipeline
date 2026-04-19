using Cbc.News.Api.Services;
using Microsoft.AspNetCore.Mvc;


namespace Cbc.News.Api.Controllers;

[ApiController]
[Route("pipeline")]
public class PipelineController : ControllerBase
{
    private readonly IngestionJobRepository _jobRepository;

    public PipelineController(IngestionJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct = default)
    {
        var jobs = await _jobRepository.GetLatestAsync(1, ct);
        var lastJob = jobs.FirstOrDefault();

        if (lastJob is null)
        {
            return Ok(new
            {
                workerStatus = "Running",
                lastFeed = "",
                storiesInserted = 0,
                storiesUpdated = 0,
                lastRun = (DateTime?)null
            });
        }

        return Ok(new
        {
            workerStatus = "Running",
            lastFeed = lastJob.Feed,
            storiesInserted = lastJob.StoriesInserted,
            storiesUpdated = lastJob.StoriesUpdated,
            lastRun = lastJob.FinishedAtUtc
        });
    }
}