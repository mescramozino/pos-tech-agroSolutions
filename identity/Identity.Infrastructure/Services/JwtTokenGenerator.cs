using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Identity.Application.Interfaces;

namespace Identity.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _config;

    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;
    }

    public string Generate(string producerId, string email)
    {
        var secret = _config["Jwt:Secret"] ?? "AgroSolutionsIdentitySecretKeyMin32Chars!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, producerId),
            new Claim(ClaimTypes.Email, email),
            new Claim("producer_id", producerId)
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "Identity.Api",
            audience: _config["Jwt:Audience"] ?? "AgroSolutions",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
