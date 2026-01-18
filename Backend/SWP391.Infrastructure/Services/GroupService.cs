using Microsoft.EntityFrameworkCore;
using SWP391.Application.DTOs.Admin;
using SWP391.Application.Interfaces;
using SWP391.Domain.Entities;
using SWP391.Infrastructure.Data;

namespace SWP391.Infrastructure.Services;

public class GroupService : IGroupService
{
    private readonly AppDbContext _context;

    public GroupService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GroupDto>> GetAllGroupsAsync()
    {
        return await _context.Groups
            .Include(g => g.Leader)
            .Include(g => g.Lecturer)
            .Include(g => g.Members)
            .Select(g => MapToDto(g))
            .ToListAsync();
    }

    public async Task<GroupDto?> GetGroupByIdAsync(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Leader)
            .Include(g => g.Lecturer)
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        return group == null ? null : MapToDto(group);
    }

    public async Task<GroupDto> CreateGroupAsync(CreateGroupDto dto)
    {
        var group = new Group
        {
            Name = dto.Name,
            Description = dto.Description,
            JiraProjectKey = dto.JiraProjectKey,
            GitHubRepoUrl = dto.GitHubRepoUrl
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        return MapToDto(group);
    }

    public async Task<GroupDto?> UpdateGroupAsync(int id, UpdateGroupDto dto)
    {
        var group = await _context.Groups
            .Include(g => g.Leader)
            .Include(g => g.Lecturer)
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null) return null;

        if (dto.Name != null) group.Name = dto.Name;
        if (dto.Description != null) group.Description = dto.Description;
        if (dto.JiraProjectKey != null) group.JiraProjectKey = dto.JiraProjectKey;
        if (dto.GitHubRepoUrl != null) group.GitHubRepoUrl = dto.GitHubRepoUrl;
        if (dto.LeaderId.HasValue) group.LeaderId = dto.LeaderId;
        if (dto.LecturerId.HasValue) group.LecturerId = dto.LecturerId;

        await _context.SaveChangesAsync();
        return MapToDto(group);
    }

    public async Task<bool> DeleteGroupAsync(int id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null) return false;

        group.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<UserListDto>> GetGroupMembersAsync(int groupId)
    {
        return await _context.Users
            .Where(u => u.GroupId == groupId && !u.IsDeleted)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                StudentCode = u.StudentCode,
                Role = u.Role.ToString(),
                GroupId = u.GroupId,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> AddMemberToGroupAsync(int groupId, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.GroupId = groupId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberFromGroupAsync(int groupId, int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.GroupId == groupId);
        if (user == null) return false;

        user.GroupId = null;
        await _context.SaveChangesAsync();
        return true;
    }

    private static GroupDto MapToDto(Group group)
    {
        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            JiraProjectKey = group.JiraProjectKey,
            GitHubRepoUrl = group.GitHubRepoUrl,
            LeaderId = group.LeaderId,
            LeaderName = group.Leader?.FullName,
            LecturerId = group.LecturerId,
            LecturerName = group.Lecturer?.FullName,
            MemberCount = group.Members?.Count ?? 0,
            CreatedAt = group.CreatedAt
        };
    }
}
