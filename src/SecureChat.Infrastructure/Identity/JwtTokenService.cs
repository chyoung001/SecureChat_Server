using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecureChat.Application.Abstractions;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Identity;

public class JwtTokenService : IJwtTokenService
{
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationDays;

    public JwtTokenService(IConfiguration configuration)
    {
        _key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!));
        _issuer = configuration["Jwt:Issuer"]!;
        _audience = configuration["Jwt:Audience"]!;
        _expirationDays = int.Parse(configuration["Jwt:ExpirationDays"] ?? "7");
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddDays(_expirationDays);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("preferred_username", user.Username),
            new Claim("tv", user.TokenVersion.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
