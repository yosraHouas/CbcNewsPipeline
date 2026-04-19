namespace Cbc.News.Api.Dtos;

public class LoginResponse
{
    public string AccessToken { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
}