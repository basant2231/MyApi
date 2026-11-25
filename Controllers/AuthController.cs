using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models.DTOs;
using MyApi.Services;

namespace MyApi.Controllers;

/// <summary>
/// AUTH CONTROLLER — Your Flutter app calls these endpoints.
/// 
/// POST /api/auth/register  → Create account (public)
/// POST /api/auth/login     → Log in (public)
/// GET  /api/auth/profile   → Get current user (requires token)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user.
    /// 
    /// Flutter usage:
    ///   final response = await http.post(
    ///     Uri.parse('http://YOUR_IP:5120/api/auth/register'),
    ///     headers: {'Content-Type': 'application/json'},
    ///     body: jsonEncode({
    ///       'username': 'john',
    ///       'email': 'john@example.com',
    ///       'password': 'secret123',
    ///     }),
    ///   );
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Log in with email and password.
    /// 
    /// Flutter usage:
    ///   final response = await http.post(
    ///     Uri.parse('http://YOUR_IP:5120/api/auth/login'),
    ///     headers: {'Content-Type': 'application/json'},
    ///     body: jsonEncode({
    ///       'email': 'john@example.com',
    ///       'password': 'secret123',
    ///     }),
    ///   );
    ///   final token = jsonDecode(response.body)['token'];
    ///   // Store this token! Send it in all future requests.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get the logged-in user's profile. REQUIRES TOKEN.
    /// 
    /// Flutter usage:
    ///   final response = await http.get(
    ///     Uri.parse('http://YOUR_IP:5120/api/auth/profile'),
    ///     headers: {
    ///       'Content-Type': 'application/json',
    ///       'Authorization': 'Bearer $token',  // ← the token from login
    ///     },
    ///   );
    /// </summary>
    [Authorize]  // ← This attribute means: "only logged-in users can access this"
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfile>> GetProfile()
    {
        // Extract user ID from the JWT token (the server does this automatically)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var profile = await _authService.GetProfileAsync(userId);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User not found." });
        }
    }
}

