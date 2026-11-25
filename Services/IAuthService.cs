using MyApi.Models.DTOs;

namespace MyApi.Services;

/// <summary>
/// Interface for authentication operations.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserProfile> GetProfileAsync(int userId);
}

