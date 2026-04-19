using Microsoft.AspNetCore.Mvc;

namespace Cbc.News.Api.Controllers;

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{
    [HttpGet("error")]
    public IActionResult ThrowError()
    {
        throw new Exception("Test error");
    }
}