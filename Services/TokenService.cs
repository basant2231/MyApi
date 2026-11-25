using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MyApi.Models;

namespace MyApi.Services;

/// <summary>
/// Creates JWT access tokens and secure refresh tokens.
/// 
/// Access Token: Short-lived (15 min), sent with every request.
/// Refresh Token: Long-lived (7 days), used only to get new access tokens.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Generate a short-lived JWT access token (15 minutes).
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique token ID
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresMinutes = int.Parse(_config["Jwt:AccessTokenExpirationMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate a cryptographically secure refresh token.
    /// Unlike JWT, this is just a random string — the server validates it
    /// by looking it up in the database.
    /// </summary>
    public RefreshToken GenerateRefreshToken(string ipAddress)
    {
        var expirationDays = int.Parse(_config["Jwt:RefreshTokenExpirationDays"] ?? "7");

        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }
}
