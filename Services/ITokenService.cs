using MyApi.Models;

namespace MyApi.Services;

/// <summary>
/// Token service interface — now handles both access and refresh tokens.
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(string ipAddress);
}
