using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MyApi.Data;
using MyApi.Middleware;
using MyApi.Services;
using Serilog;

// ───────────── SERILOG BOOTSTRAP ─────────────
// Structured logging — much better than Console.WriteLine / print()
// Logs to console AND files with automatic rotation
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_485_760) // 10 MB per file
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 10_485_760));

    // ───────────── DATABASE ─────────────
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ───────────── JWT AUTHENTICATION ─────────────
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                // Reduce clock skew from default 5 min to 30 seconds
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // Return proper JSON error for unauthorized requests (not just 401)
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        success = false,
                        message = "You are not authenticated. Please login first.",
                        statusCode = 401
                    };

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(response, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }));
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        success = false,
                        message = "You do not have permission to access this resource.",
                        statusCode = 403
                    };

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(response, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }));
                }
            };
        });

    builder.Services.AddAuthorization();

    // ───────────── RATE LIMITING ─────────────
    // Prevents brute force attacks and API abuse
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Return proper JSON for rate-limited requests
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            var response = new
            {
                success = false,
                message = "Too many requests. Please try again later.",
                statusCode = 429
            };
            await context.HttpContext.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                cancellationToken);
        };

        // General API rate limit: 100 requests per minute per IP
        options.AddPolicy("general", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                }));

        // Strict rate limit for auth endpoints: 10 requests per minute per IP
        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                }));
    });

    // ───────────── REGISTER SERVICES ─────────────
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IBookService, BookService>();

    // Register all FluentValidation validators from this assembly
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // ───────────── CONTROLLERS & SWAGGER ─────────────
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Consistent JSON formatting across all responses
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddOpenApi();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MyApi",
            Version = "v1",
            Description = "Production-ready .NET Web API with JWT Authentication, " +
                          "Rate Limiting, Account Lockout, and Refresh Token Rotation"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste your JWT access token here (without 'Bearer ' prefix)"
        });

        options.AddSecurityRequirement(doc =>
        {
            var requirement = new OpenApiSecurityRequirement();
            var schemeRef = new OpenApiSecuritySchemeReference("Bearer", doc);
            requirement[schemeRef] = new List<string>();
            return requirement;
        });
    });

    // ───────────── CORS ─────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Development", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });

        // Tighter production CORS — update AllowedOrigins in appsettings.Production.json
        options.AddPolicy("Production", policy =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? ["http://localhost"];

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // ───────────── HEALTH CHECKS ─────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>();

    var app = builder.Build();

    // ───────────── AUTO-CREATE DATABASE ─────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    // ───────────── MIDDLEWARE PIPELINE ─────────────
    // Order matters! Each middleware runs in this exact order.

    // 1) Security headers on EVERY response
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // 2) Global exception handler — catches all unhandled errors
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // 3) Serilog request logging — log every HTTP request
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RemoteIp", httpContext.Connection.RemoteIpAddress);
        };
    });

    // 4) Swagger UI (dev only)
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyApi v1");
            options.RoutePrefix = string.Empty;
        });
    }

    // 5) CORS — use different policies per environment
    var corsPolicy = app.Environment.IsDevelopment() ? "Development" : "Production";
    app.UseCors(corsPolicy);

    // 6) Rate limiting
    app.UseRateLimiter();

    // 7) Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // 8) Map controllers and health check endpoint
    app.MapControllers()
       .RequireRateLimiting("general"); // Apply general rate limit to all endpoints

    app.MapHealthChecks("/health");

    Log.Information("MyApi started successfully on {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
