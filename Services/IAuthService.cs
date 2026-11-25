using MyApi.Models.DTOs;

namespace MyApi.Services;

/// <summary>
/// Auth service interface — now includes refresh tokens, logout, password change.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress);
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task RevokeTokenAsync(string refreshToken, string ipAddress);
    Task<UserProfile> GetProfileAsync(int userId);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
}
