using System.Security.Claims;
using System.Text;
using Cbc.News.Api.Models;
using Cbc.News.Api.Options;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Cbc.News.Api.Services;

public class TokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(AppUser user)
    {
        if (string.IsNullOrWhiteSpace(_jwtSettings.Key))
            throw new InvalidOperationException("JWT signing key is missing.");

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Username),
        new(JwtRegisteredClaimNames.UniqueName, user.Username),
        new(ClaimTypes.NameIdentifier, user.Id ?? ""),
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.Role, user.Role)
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenValue, expiresAtUtc);
    }
}