namespace MyApi.Models.DTOs;

/// <summary>
/// Safe user data to return (never expose password hash).
/// </summary>
public class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

