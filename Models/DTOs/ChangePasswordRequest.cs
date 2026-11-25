namespace MyApi.Models.DTOs;

/// <summary>
/// Request to change password (requires current password for security).
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

