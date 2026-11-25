using System.ComponentModel.DataAnnotations;

namespace MyApi.Models.DTOs;

/// <summary>
/// What the client sends when logging in.
/// </summary>
public class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

