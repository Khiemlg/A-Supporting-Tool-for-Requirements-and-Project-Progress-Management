using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP391.Infrastructure.Data;
using System.Security.Claims;

namespace SWP391.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MemberController : ControllerBase
{
    private readonly AppDbContext _context;

    public MemberController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetCurrentUserId();

        var stats = new
        {
            TotalTasks = await _context.Tasks.CountAsync(t => t.AssigneeId == userId && !t.IsDeleted),
            CompletedTasks = await _context.Tasks.CountAsync(t => t.AssigneeId == userId && !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.Done),
            InProgressTasks = await _context.Tasks.CountAsync(t => t.AssigneeId == userId && !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.InProgress),
            TodoTasks = await _context.Tasks.CountAsync(t => t.AssigneeId == userId && !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.Todo),
            CommitCount = await _context.GitHubCommits.CountAsync(c => c.UserId == userId)
        };

        return Ok(stats);
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks()
    {
        var userId = GetCurrentUserId();

        var tasks = await _context.Tasks
            .Where(t => t.AssigneeId == userId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                Status = t.Status.ToString(),
                t.Priority,
                t.DueDate
            })
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPatch("tasks/{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var userId = GetCurrentUserId();
        var task = await _context.Tasks.FindAsync(id);
        
        if (task == null || task.IsDeleted) 
            return NotFound();
        
        if (task.AssigneeId != userId)
            return Unauthorized("You can only update your own tasks");

        if (Enum.TryParse<Domain.Enums.TaskStatus>(dto.Status, out var status))
        {
            task.Status = status;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Status updated" });
        }

        return BadRequest("Invalid status");
    }

    [HttpGet("my-commits")]
    public async Task<IActionResult> GetMyCommits()
    {
        var userId = GetCurrentUserId();

        var commits = await _context.GitHubCommits
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CommitDate)
            .Take(50)
            .Select(c => new
            {
                c.Id,
                c.CommitSha,
                c.Message,
                CommittedAt = c.CommitDate,
                c.Additions,
                c.Deletions,
                CommitUrl = c.Url
            })
            .ToListAsync();

        return Ok(commits);
    }
}

// UpdateStatusDto is defined in TeamLeaderController.cs
