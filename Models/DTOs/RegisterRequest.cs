using System.ComponentModel.DataAnnotations;

namespace MyApi.Models.DTOs;

/// <summary>
/// What the client sends when registering.
/// Like a Dart class with fields: username, email, password.
/// </summary>
public class RegisterRequest
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

