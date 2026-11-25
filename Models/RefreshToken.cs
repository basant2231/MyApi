namespace MyApi.Models;

/// <summary>
/// Refresh token entity — stored in the database.
/// 
/// WHY REFRESH TOKENS?
/// Access tokens (JWT) expire in 15 minutes. Without refresh tokens,
/// the user would have to log in again every 15 minutes.
/// 
/// Instead: when the access token expires, the Flutter app sends the
/// refresh token to get a NEW access token — no login required.
/// 
/// Security: refresh tokens are single-use (rotation). Once used,
/// the old one is revoked and a new one is issued.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByIp { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? RevokeReason { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;
}

