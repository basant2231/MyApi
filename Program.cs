using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApi.Data;
using MyApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ───────────── DATABASE ─────────────
// SQLite = simple file-based database (like shared_preferences but for a server).
// The database file "app.db" will be created automatically in the project folder.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ───────────── JWT AUTHENTICATION ─────────────
// JWT = JSON Web Token. It's how your Flutter app proves "I am user X".
// Flow: Login → get token → store token → send token with every request.
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ───────────── REGISTER SERVICES ─────────────
// Dependency Injection — like GetIt or Provider in Flutter.
// "Scoped" = one instance per HTTP request.
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ───────────── CONTROLLERS & SWAGGER ─────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ───────────── CORS ─────────────
// CORS allows your Flutter app (running on a different port/device) to call this API.
// Without this, the browser/emulator would block the requests.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()    // Allow requests from any origin (Flutter app, browser, etc.)
              .AllowAnyMethod()    // Allow GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader();   // Allow any headers (including Authorization)
    });
});

var app = builder.Build();

// ───────────── AUTO-CREATE DATABASE ─────────────
// This creates the database tables automatically when the app starts.
// In production, you'd use "migrations" instead, but this is simpler for learning.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ───────────── MIDDLEWARE PIPELINE ─────────────
// Order matters! CORS → Auth → Controllers
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();             // 1) Allow cross-origin requests
app.UseAuthentication();   // 2) Check JWT tokens
app.UseAuthorization();    // 3) Check [Authorize] attributes

app.MapControllers();

app.Run();
