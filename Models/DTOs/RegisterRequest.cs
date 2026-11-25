using System.ComponentModel.DataAnnotations;

namespace MyApi.Models.DTOs;

/// <summary>
/// What the client sends when registering.
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
    [MaxLength(30, ErrorMessage = "Username must not exceed 30 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$",
        ErrorMessage = "Username can only contain letters, numbers, and underscores.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(254, ErrorMessage = "Email must not exceed 254 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
    public string Password { get; set; } = string.Empty;
}
