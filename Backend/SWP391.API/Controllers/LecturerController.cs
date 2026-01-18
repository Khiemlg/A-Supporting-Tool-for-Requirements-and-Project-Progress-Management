using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP391.Infrastructure.Data;
using System.Security.Claims;

namespace SWP391.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Lecturer,Admin")]
public class LecturerController : ControllerBase
{
    private readonly AppDbContext _context;

    public LecturerController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    [HttpGet("my-groups")]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = GetCurrentUserId();
        
        var groups = await _context.Groups
            .Where(g => g.LecturerId == userId && !g.IsDeleted)
            .Include(g => g.Members.Where(m => !m.IsDeleted))
            .Include(g => g.Tasks)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.JiraProjectKey,
                g.GitHubRepoUrl,
                MemberCount = g.Members.Count(m => !m.IsDeleted),
                LeaderName = g.Leader != null ? g.Leader.FullName : null,
                TaskCount = g.Tasks.Count(t => !t.IsDeleted),
                CompletedTasks = g.Tasks.Count(t => !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.Done),
                Members = g.Members.Where(m => !m.IsDeleted).Select(m => new
                {
                    m.Id,
                    m.FullName,
                    m.Email,
                    m.StudentCode,
                    Role = m.Role.ToString()
                }).ToList()
            })
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("progress-reports")]
    public async Task<IActionResult> GetProgressReports()
    {
        var userId = GetCurrentUserId();
        
        var reports = await _context.Groups
            .Where(g => g.LecturerId == userId && !g.IsDeleted)
            .Include(g => g.Tasks)
            .Include(g => g.GitHubCommits)
            .Select(g => new
            {
                GroupId = g.Id,
                GroupName = g.Name,
                TotalTasks = g.Tasks.Count(t => !t.IsDeleted),
                CompletedTasks = g.Tasks.Count(t => !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.Done),
                InProgressTasks = g.Tasks.Count(t => !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.InProgress),
                PendingTasks = g.Tasks.Count(t => !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.Todo),
                CompletionPercentage = g.Tasks.Any(t => !t.IsDeleted) 
                    ? (int)Math.Round((double)g.Tasks.Count(t => !t.IsDeleted && t.Status == Domain.Enums.TaskStatus.Done) / g.Tasks.Count(t => !t.IsDeleted) * 100)
                    : 0,
                TotalCommits = g.GitHubCommits.Count,
                LastActivityDate = g.Tasks.Where(t => !t.IsDeleted).Max(t => (DateTime?)t.UpdatedAt) 
                    ?? g.GitHubCommits.Max(c => (DateTime?)c.CommitDate)
            })
            .ToListAsync();

        return Ok(reports);
    }

    [HttpGet("groups/{groupId}/commits")]
    public async Task<IActionResult> GetGroupCommits(int groupId)
    {
        var userId = GetCurrentUserId();
        
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId && g.LecturerId == userId && !g.IsDeleted);
        
        if (group == null)
            return NotFound("Group not found or you don't have access");

        var commits = await _context.GitHubCommits
            .Where(c => c.GroupId == groupId)
            .Include(c => c.User)
            .OrderByDescending(c => c.CommitDate)
            .Take(50)
            .Select(c => new
            {
                c.Id,
                c.CommitSha,
                c.Message,
                c.AuthorName,
                AuthorFullName = c.User != null ? c.User.FullName : c.AuthorName,
                CommittedAt = c.CommitDate,
                c.Additions,
                c.Deletions,
                CommitUrl = c.Url
            })
            .ToListAsync();

        return Ok(commits);
    }
}
