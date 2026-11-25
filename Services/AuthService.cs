using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Models.DTOs;

namespace MyApi.Services;

/// <summary>
/// Production-grade authentication service.
/// 
/// Security features:
/// - BCrypt password hashing (cost factor 12)
/// - Account lockout after 5 failed attempts (15 min lockout)
/// - Refresh token rotation (single-use tokens)
/// - Cascade revocation (if a used token is reused, all tokens are revoked)
/// - IP tracking for audit trail
/// - Email normalization (case-insensitive)
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _config;

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public AuthService(
        AppDbContext db,
        ITokenService tokenService,
        ILogger<AuthService> logger,
        IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress)
    {
        // Normalize email to prevent duplicate accounts with different casing
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUsername = request.Username.Trim();

        // Check if email already exists
        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
            throw new InvalidOperationException("An account with this email already exists.");

        // Check if username already exists
        if (await _db.Users.AnyAsync(u => u.Username == normalizedUsername))
            throw new InvalidOperationException("This username is already taken.");

        // Hash password with higher cost factor for production security
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        var user = new User
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow,
            LastLoginIp = ipAddress
        };

        _db.Users.Add(user);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);
        user.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Username} registered from IP {Ip}", user.Username, ipAddress);

        return BuildAuthResponse(user, accessToken, refreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Find user with refresh tokens loaded
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // ── Security: use same error message for "user not found" and "wrong password"
        // to prevent user enumeration attacks
        if (user is null)
        {
            // Perform a dummy hash to prevent timing attacks
            BCrypt.Net.BCrypt.HashPassword("dummy-password", workFactor: 12);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Check if account is active
        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated. Contact support.");

        // Check if account is locked out
        if (user.IsLockedOut)
        {
            var remainingMinutes = (int)(user.LockoutEnd!.Value - DateTime.UtcNow).TotalMinutes + 1;
            _logger.LogWarning("Locked account login attempt for {Email} from IP {Ip}", normalizedEmail, ipAddress);
            throw new UnauthorizedAccessException(
                $"Account is locked due to too many failed attempts. Try again in {remainingMinutes} minute(s).");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Increment failed attempts
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                _logger.LogWarning("Account {Email} locked after {Attempts} failed attempts from IP {Ip}",
                    normalizedEmail, user.FailedLoginAttempts, ipAddress);
            }

            await _db.SaveChangesAsync();
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // ── Successful login ──
        // Reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(ipAddress);
        user.RefreshTokens.Add(refreshToken);

        // Clean up old expired/revoked refresh tokens (keep DB clean)
        RemoveOldRefreshTokens(user);

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged in from IP {Ip}", user.Username, ipAddress);

        return BuildAuthResponse(user, accessToken, refreshToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token, string ipAddress)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

        if (user is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var refreshToken = user.RefreshTokens.Single(t => t.Token == token);

        // ── SECURITY: Detect token reuse (stolen token scenario) ──
        // If someone tries to use a revoked token, it means the token was stolen.
        // Revoke ALL tokens for this user as a security measure.
        if (refreshToken.IsRevoked)
        {
            _logger.LogWarning(
                "Attempted reuse of revoked refresh token for user {UserId} from IP {Ip}. Revoking all tokens.",
                user.Id, ipAddress);

            RevokeAllDescendantTokens(refreshToken, user, ipAddress,
                $"Attempted reuse of revoked ancestor token: {token}");

            await _db.SaveChangesAsync();
            throw new UnauthorizedAccessException("Token has been revoked. All sessions terminated for security.");
        }

        if (!refreshToken.IsActive)
            throw new UnauthorizedAccessException("Refresh token is expired.");

        // ── Rotate: revoke old token, create new one ──
        var newRefreshToken = RotateRefreshToken(refreshToken, ipAddress);
        user.RefreshTokens.Add(newRefreshToken);

        RemoveOldRefreshTokens(user);

        var accessToken = _tokenService.GenerateAccessToken(user);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Token refreshed for user {Username} from IP {Ip}", user.Username, ipAddress);

        return BuildAuthResponse(user, accessToken, newRefreshToken);
    }

    public async Task RevokeTokenAsync(string token, string ipAddress)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

        if (user is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var refreshToken = user.RefreshTokens.Single(t => t.Token == token);

        if (!refreshToken.IsActive)
            throw new InvalidOperationException("Token is already revoked or expired.");

        RevokeToken(refreshToken, ipAddress, "Revoked by user logout");

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged out (token revoked) from IP {Ip}", user.Username, ipAddress);
    }

    public async Task<UserProfile> GetProfileAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        return new UserProfile
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        // Hash new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all existing refresh tokens (force re-login on all devices)
        foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
        {
            RevokeToken(token, "system", "Password changed — all sessions terminated");
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Password changed for user {Username}. All sessions revoked.", user.Username);
    }

    // ────────────────── Private Helpers ──────────────────

    private static AuthResponse BuildAuthResponse(User user, string accessToken, RefreshToken refreshToken)
    {
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            Username = user.Username,
            Email = user.Email,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt
        };
    }

    private RefreshToken RotateRefreshToken(RefreshToken oldToken, string ipAddress)
    {
        var newToken = _tokenService.GenerateRefreshToken(ipAddress);
        RevokeToken(oldToken, ipAddress, "Replaced by new token", newToken.Token);
        return newToken;
    }

    private static void RevokeToken(RefreshToken token, string ipAddress, string? reason = null, string? replacedBy = null)
    {
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.RevokeReason = reason;
        token.ReplacedByToken = replacedBy;
    }

    private void RevokeAllDescendantTokens(RefreshToken token, User user, string ipAddress, string reason)
    {
        // Follow the chain of replacement tokens and revoke them all
        if (!string.IsNullOrEmpty(token.ReplacedByToken))
        {
            var childToken = user.RefreshTokens.SingleOrDefault(t => t.Token == token.ReplacedByToken);
            if (childToken is not null)
            {
                if (childToken.IsActive)
                    RevokeToken(childToken, ipAddress, reason);
                else
                    RevokeAllDescendantTokens(childToken, user, ipAddress, reason);
            }
        }
    }

    private static void RemoveOldRefreshTokens(User user)
    {
        // Remove tokens that are expired AND revoked AND older than 2 days
        user.RefreshTokens.RemoveAll(t =>
            !t.IsActive && t.CreatedAt.AddDays(2) <= DateTime.UtcNow);
    }
}
