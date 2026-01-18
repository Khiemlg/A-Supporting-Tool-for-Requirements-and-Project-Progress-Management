using SWP391.Application.DTOs.Auth;

namespace SWP391.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<UserDto?> GetCurrentUserAsync(int userId);
}

public interface ITokenService
{
    string GenerateToken(int userId, string email, string role);
    int? ValidateToken(string token);
}

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
