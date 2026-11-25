using System.ComponentModel.DataAnnotations;

namespace MyApi.Models.DTOs;

/// <summary>
/// Request to refresh an expired access token.
/// The Flutter app sends the stored refresh token to get a new access token.
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}

