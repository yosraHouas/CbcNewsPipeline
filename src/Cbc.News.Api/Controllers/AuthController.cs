using Cbc.News.Api.Dtos;
using Cbc.News.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cbc.News.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly TokenService _tokenService;

    public AuthController(
        UserRepository userRepository,
        TokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
     [FromBody] LoginRequest request,
     CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var username = request.Username.Trim();

        var user = await _userRepository.GetByUsernameAsync(username, ct);

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!passwordValid)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var (token, expiresAtUtc) = _tokenService.CreateToken(user);

        var response = new LoginResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            Username = user.Username,
            Role = user.Role
        };

        return Ok(response);
    }
}