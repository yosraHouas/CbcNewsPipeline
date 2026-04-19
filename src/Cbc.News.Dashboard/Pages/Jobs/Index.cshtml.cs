using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cbc.News.Dashboard.Pages.Jobs;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        var token = HttpContext.Session.GetString("token");

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Login");
        }

        return Page();
    }
}