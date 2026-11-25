using MyApi.Models;

namespace MyApi.Services;

/// <summary>
/// Interface for token generation.
/// In C#, interfaces define "what" a class must do (like abstract classes in Dart).
/// </summary>
public interface ITokenService
{
    string GenerateToken(User user);
}

