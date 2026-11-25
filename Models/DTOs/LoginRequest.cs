using System.ComponentModel.DataAnnotations;

namespace MyApi.Models.DTOs;

/// <summary>
/// What the client sends when logging in.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
    public string Password { get; set; } = string.Empty;
}
