using Microsoft.EntityFrameworkCore;
using SWP391.Application.DTOs.Auth;
using SWP391.Application.Interfaces;
using SWP391.Domain.Entities;
using SWP391.Domain.Enums;
using SWP391.Infrastructure.Data;

namespace SWP391.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;

    public AuthService(
        AppDbContext context, 
        ITokenService tokenService, 
        IPasswordService passwordService)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordService = passwordService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        if (user == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email không tồn tại trong hệ thống"
            };
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Mật khẩu không chính xác"
            };
        }

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return new AuthResponseDto
        {
            Success = true,
            Message = "Đăng nhập thành công",
            Token = token,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        // Validate
        if (request.Password != request.ConfirmPassword)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Mật khẩu xác nhận không khớp"
            };
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email đã được sử dụng"
            };
        }

        // Map RoleId to UserRole enum (1=Admin, 2=Lecturer, 3=TeamLeader, 4=TeamMember)
        var role = request.RoleId switch
        {
            1 => UserRole.Admin,
            2 => UserRole.Lecturer,
            3 => UserRole.TeamLeader,
            _ => UserRole.TeamMember
        };

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            FullName = request.FullName,
            StudentCode = request.StudentCode,
            PhoneNumber = request.PhoneNumber,
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return new AuthResponseDto
        {
            Success = true,
            Message = "Đăng ký thành công",
            Token = token,
            User = MapToUserDto(user)
        };
    }

    public async Task<UserDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        return user == null ? null : MapToUserDto(user);
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            StudentCode = user.StudentCode,
            Role = user.Role.ToString(),
            GroupId = user.GroupId,
            GroupName = user.Group?.Name
        };
    }
}
