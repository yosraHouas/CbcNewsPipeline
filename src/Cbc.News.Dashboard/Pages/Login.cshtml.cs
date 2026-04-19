using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cbc.News.Dashboard.Pages;

public class LoginModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public LoginModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    [Required]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = _httpClientFactory.CreateClient("api");

        var request = new
        {
            username = Username,
            password = Password
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/auth/login", content);

        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Nom d'utilisateur ou mot de passe invalide.";
            return Page();
        }

        var json = await response.Content.ReadAsStringAsync();

        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (loginResponse is null || string.IsNullOrWhiteSpace(loginResponse.AccessToken))
        {
            ErrorMessage = "Réponse de connexion invalide.";
            return Page();
        }

        HttpContext.Session.SetString("token", loginResponse.AccessToken);
        HttpContext.Session.SetString("role", loginResponse.Role ?? string.Empty);
        HttpContext.Session.SetString("username", loginResponse.Username ?? string.Empty);

        return RedirectToPage("/Index");
    }

    public sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}