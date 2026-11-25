namespace MyApi.Models;

/// <summary>
/// The User entity stored in the database.
/// Now includes security fields for account lockout and tracking.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ── Security Fields ──
    /// <summary>Tracks consecutive failed login attempts.</summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>When the lockout ends. Null = not locked out.</summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>Whether the account is active (admin can deactivate).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Last successful login timestamp.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>IP address of last login.</summary>
    public string? LastLoginIp { get; set; }

    // ── Navigation Properties ──
    public List<RefreshToken> RefreshTokens { get; set; } = [];

    // ── Helpers ──
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
}
