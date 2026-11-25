namespace MyApi.Middleware;

/// <summary>
/// Adds security headers to every HTTP response.
/// 
/// These headers protect against common web attacks:
/// - XSS (Cross-Site Scripting)
/// - Clickjacking
/// - MIME type sniffing
/// - Information leakage
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var isSwagger = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                     || path == "/"
                     || path.StartsWith("/index.html", StringComparison.OrdinalIgnoreCase);

        // Prevent browsers from MIME-type sniffing (security risk)
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Prevent your API from being embedded in an iframe (clickjacking protection)
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Enable browser XSS filter
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Control what info is sent in the Referer header
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Don't reveal server technology
        context.Response.Headers["X-Powered-By"] = "";
        context.Response.Headers.Remove("Server");

        // Permissions policy — disable dangerous browser features
        context.Response.Headers["Permissions-Policy"] =
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

        // Content Security Policy — relaxed for Swagger UI, strict for API
        if (!isSwagger)
        {
            context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }

        await _next(context);
    }
}

