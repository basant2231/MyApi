using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models;
using MyApi.Models.DTOs;
using MyApi.Services;

namespace MyApi.Controllers;

/// <summary>
/// Production-grade AUTH CONTROLLER.
/// 
/// POST /api/auth/register      → Create account (rate limited)
/// POST /api/auth/login         → Log in (rate limited)
/// POST /api/auth/refresh-token → Refresh expired access token
/// POST /api/auth/logout        → Revoke refresh token (logout)
/// GET  /api/auth/profile       → Get current user profile
/// POST /api/auth/change-password → Change password (revokes all sessions)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Fail(
                "Validation failed.",
                400,
                validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var response = await _authService.RegisterAsync(request, GetIpAddress());
            return StatusCode(201, ApiResponse<AuthResponse>.Created(response, "Account created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Log in with email and password.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Fail(
                "Validation failed.",
                400,
                validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var response = await _authService.LoginAsync(request, GetIpAddress());
            return Ok(ApiResponse<AuthResponse>.Ok(response, "Login successful."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail(ex.Message, 401));
        }
    }

    /// <summary>
    /// Get a new access token using a refresh token.
    /// 
    /// Call this when the access token expires (401 response).
    /// The old refresh token is revoked and a new one is returned.
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var validation = await _refreshTokenValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Fail(
                "Validation failed.",
                400,
                validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken, GetIpAddress());
            return Ok(ApiResponse<AuthResponse>.Ok(response, "Token refreshed successfully."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail(ex.Message, 401));
        }
    }

    /// <summary>
    /// Logout — revoke the refresh token so it can't be used again.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _authService.RevokeTokenAsync(request.RefreshToken, GetIpAddress());
            return Ok(ApiResponse.Ok("Logged out successfully."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail(ex.Message, 401));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get the logged-in user's profile. REQUIRES ACCESS TOKEN.
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfile>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized(ApiResponse.Fail("Invalid token.", 401));

        try
        {
            var profile = await _authService.GetProfileAsync(userId.Value);
            return Ok(ApiResponse<UserProfile>.Ok(profile));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail("User not found.", 404));
        }
    }

    /// <summary>
    /// Change password. Revokes all refresh tokens (logs out all devices).
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Fail(
                "Validation failed.",
                400,
                validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var userId = GetUserId();
        if (userId is null) return Unauthorized(ApiResponse.Fail("Invalid token.", 401));

        try
        {
            await _authService.ChangePasswordAsync(userId.Value, request);
            return Ok(ApiResponse.Ok("Password changed successfully. Please log in again."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail(ex.Message, 401));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse.Fail("User not found.", 404));
        }
    }

    // ────────────────── Private Helpers ──────────────────

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && int.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// Get the real client IP address, handling proxies/load balancers.
    /// </summary>
    private string GetIpAddress()
    {
        // Check for forwarded header (behind proxy/load balancer)
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip)) return ip;
        }

        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }
}
