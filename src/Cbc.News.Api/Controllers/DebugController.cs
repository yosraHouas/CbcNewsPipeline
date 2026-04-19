using Microsoft.AspNetCore.Mvc;

namespace Cbc.News.Api.Controllers;

[ApiController]
[Route("debug")]
public class DebugController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DebugController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("mongo")]
    public IActionResult GetMongoConfig()
    {
        return Ok(new
        {
            connectionString = _configuration["Mongo:ConnectionString"],
            database = _configuration["Mongo:Database"]
        });
    }
}