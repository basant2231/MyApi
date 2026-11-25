namespace MyApi.Models.DTOs;

/// <summary>
/// What the server sends back after login/register.
/// Your Flutter app will parse this JSON response.
/// </summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

