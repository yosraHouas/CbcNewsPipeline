using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cbc.News.Dashboard.Pages;

public class IndexModel : PageModel
{
    public bool IsAdmin { get; private set; }

    public IActionResult OnGet()
    {
        var token = HttpContext.Session.GetString("token");

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Login");
        }

        var role = HttpContext.Session.GetString("role");
        IsAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

        return Page();
    }
}