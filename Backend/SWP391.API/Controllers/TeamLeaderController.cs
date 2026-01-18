using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP391.Domain.Entities;
using SWP391.Infrastructure.Data;
using System.Security.Claims;

namespace SWP391.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "TeamLeader,Admin")]
public class TeamLeaderController : ControllerBase
{
    private readonly AppDbContext _context;

    public TeamLeaderController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    private async Task<int?> GetUserGroupId()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        return user?.GroupId;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var groupId = await GetUserGroupId();
        if (groupId == null)
            return Ok(new { TotalTasks = 0, CompletedTasks = 0, InProgressTasks = 0, TodoTasks = 0, MemberCount = 0, TotalRequirements = 0 });

        var stats = new
        {
            TotalTasks = await _context.Tasks.CountAsync(t => t.GroupId == groupId && !t.IsDeleted),
            CompletedTasks = await _context.Tasks.CountAsync(t => t.GroupId == groupId && !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.Done),
            InProgressTasks = await _context.Tasks.CountAsync(t => t.GroupId == groupId && !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.InProgress),
            TodoTasks = await _context.Tasks.CountAsync(t => t.GroupId == groupId && !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.Todo),
            MemberCount = await _context.Users.CountAsync(u => u.GroupId == groupId && !u.IsDeleted),
            TotalRequirements = await _context.Requirements.CountAsync(r => r.GroupId == groupId && !r.IsDeleted),
        };

        return Ok(stats);
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks()
    {
        var groupId = await GetUserGroupId();
        if (groupId == null) return Ok(new List<object>());

        var tasks = await _context.Tasks
            .Where(t => t.GroupId == groupId && !t.IsDeleted)
            .Include(t => t.Assignee)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                Status = t.Status.ToString(),
                t.Priority,
                t.DueDate,
                AssigneeName = t.Assignee != null ? t.Assignee.FullName : null,
                t.AssigneeId
            })
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
    {
        var groupId = await GetUserGroupId();
        if (groupId == null) return BadRequest("User không thuộc nhóm nào");

        var task = new ProjectTask
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority.ToString(),
            DueDate = string.IsNullOrEmpty(dto.DueDate) ? null : DateTime.Parse(dto.DueDate),
            AssigneeId = dto.AssigneeId > 0 ? dto.AssigneeId : null,
            GroupId = groupId.Value,
            Status = Domain.Enums.TaskStatus.Todo
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return Ok(new { task.Id, Message = "Task created successfully" });
    }

    [HttpPatch("tasks/{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null || task.IsDeleted) return NotFound();

        if (Enum.TryParse<Domain.Enums.TaskStatus>(dto.Status, out var status))
        {
            task.Status = status;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Status updated" });
        }

        return BadRequest("Invalid status");
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers()
    {
        var groupId = await GetUserGroupId();
        if (groupId == null) return Ok(new List<object>());

        var members = await _context.Users
            .Where(u => u.GroupId == groupId && !u.IsDeleted)
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync();

        return Ok(members);
    }

    [HttpGet("members-stats")]
    public async Task<IActionResult> GetMembersStats()
    {
        var groupId = await GetUserGroupId();
        if (groupId == null) return Ok(new List<object>());

        var members = await _context.Users
            .Where(u => u.GroupId == groupId && !u.IsDeleted)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.StudentCode,
                Role = u.Role.ToString(),
                TaskCount = _context.Tasks.Count(t => t.AssigneeId == u.Id && !t.IsDeleted),
                CompletedTasks = _context.Tasks.Count(t => t.AssigneeId == u.Id && !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.Done),
                CommitCount = _context.GitHubCommits.Count(c => c.UserId == u.Id)
            })
            .ToListAsync();

        return Ok(members);
    }

    [HttpGet("requirements")]
    public async Task<IActionResult> GetRequirements()
    {
        var groupId = await GetUserGroupId();
        if (groupId == null) return Ok(new List<object>());

        var requirements = await _context.Requirements
            .Where(r => r.GroupId == groupId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Description,
                r.JiraIssueKey,
                r.JiraIssueUrl,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(requirements);
    }

    [HttpPost("requirements")]
    public async Task<IActionResult> CreateRequirement([FromBody] CreateRequirementDto dto)
    {
        var groupId = await GetUserGroupId();
        if (groupId == null) return BadRequest("User không thuộc nhóm nào");

        var requirement = new Requirement
        {
            Title = dto.Title,
            Description = dto.Description,
            GroupId = groupId.Value
        };

        _context.Requirements.Add(requirement);
        await _context.SaveChangesAsync();

        return Ok(new { requirement.Id, Message = "Requirement created successfully" });
    }
}

public record CreateTaskDto(string Title, string? Description, int Priority, string? DueDate, int AssigneeId);
public record UpdateStatusDto(string Status);
public record CreateRequirementDto(string Title, string? Description);
