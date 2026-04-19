using Cbc.News.Dashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cbc.News.Dashboard.Controllers;

[Route("Events")]
public class EventsController : Controller
{
    private readonly EventService _service;

    public EventsController(EventService service)
    {
        _service = service;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var events = await _service.GetLatestAsync();
        return View(events);
    }
}