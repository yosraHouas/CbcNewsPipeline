using Cbc.News.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbc.News.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("jobs")]
public class JobsController : ControllerBase
{
    private readonly IngestionJobRepository _jobRepository;

    public JobsController(IngestionJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var jobs = await _jobRepository.GetLatestAsync(limit, ct);

        return Ok(new
        {
            count = jobs.Count,
            limit,
            items = jobs
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobById(
        string id,
        CancellationToken ct = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, ct);

        if (job is null)
            return NotFound(new { message = $"Job '{id}' not found." });

        return Ok(job);
    }
}