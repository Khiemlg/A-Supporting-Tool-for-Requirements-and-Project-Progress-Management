using Microsoft.EntityFrameworkCore;
using SWP391.Application.DTOs.Admin;
using SWP391.Application.Interfaces;
using SWP391.Domain.Enums;
using SWP391.Infrastructure.Data;

namespace SWP391.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _context;

    public UserManagementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserListDto>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Group)
            .Where(u => !u.IsDeleted)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                StudentCode = u.StudentCode,
                Role = u.Role.ToString(),
                GroupId = u.GroupId,
                GroupName = u.Group != null ? u.Group.Name : null,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<UserListDto>> GetUsersByRoleAsync(string role)
    {
        if (!Enum.TryParse<UserRole>(role, out var userRole))
            return Enumerable.Empty<UserListDto>();

        return await _context.Users
            .Include(u => u.Group)
            .Where(u => u.Role == userRole && !u.IsDeleted)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                StudentCode = u.StudentCode,
                Role = u.Role.ToString(),
                GroupId = u.GroupId,
                GroupName = u.Group != null ? u.Group.Name : null,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<UserListDto>> GetLecturersAsync()
    {
        return await GetUsersByRoleAsync("Lecturer");
    }

    public async Task<UserListDto?> UpdateUserRoleAsync(UpdateUserRoleDto dto)
    {
        if (!Enum.TryParse<UserRole>(dto.Role, out var newRole))
            return null;

        var user = await _context.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted);

        if (user == null) return null;

        user.Role = newRole;
        await _context.SaveChangesAsync();

        return new UserListDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            StudentCode = user.StudentCode,
            Role = user.Role.ToString(),
            GroupId = user.GroupId,
            GroupName = user.Group?.Name,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> AssignUserToGroupAsync(AssignUserToGroupDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null) return false;

        user.GroupId = dto.GroupId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignLecturerToGroupAsync(AssignLecturerDto dto)
    {
        var group = await _context.Groups.FindAsync(dto.GroupId);
        var lecturer = await _context.Users.FindAsync(dto.LecturerId);
        
        if (group == null || lecturer == null) return false;
        if (lecturer.Role != UserRole.Lecturer) return false;

        group.LecturerId = dto.LecturerId;
        await _context.SaveChangesAsync();
        return true;
    }
}
