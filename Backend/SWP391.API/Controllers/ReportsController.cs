using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP391.Infrastructure.Data;
using System.Text;

namespace SWP391.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generate SRS Document from requirements
    /// </summary>
    [HttpGet("srs/{groupId}")]
    public async Task<IActionResult> GenerateSRS(int groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Requirements)
            .Include(g => g.Members)
            .Include(g => g.Lecturer)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

        if (group == null) return NotFound("Group not found");

        var requirements = group.Requirements.Where(r => !r.IsDeleted).OrderBy(r => r.Id).ToList();
        var members = group.Members.Where(m => !m.IsDeleted).ToList();

        var srs = new
        {
            DocumentTitle = $"Software Requirements Specification - {group.Name}",
            GeneratedAt = DateTime.Now,
            ProjectInfo = new
            {
                group.Name,
                group.Description,
                group.JiraProjectKey,
                LecturerName = group.Lecturer?.FullName,
                TeamSize = members.Count
            },
            TeamMembers = members.Select(m => new
            {
                m.FullName,
                m.Email,
                m.StudentCode,
                Role = m.Role.ToString()
            }),
            Requirements = requirements.Select((r, index) => new
            {
                Id = $"REQ-{(index + 1).ToString("D3")}",
                r.Title,
                r.Description,
                r.Priority,
                r.Status,
                r.JiraIssueKey
            }),
            Summary = new
            {
                TotalRequirements = requirements.Count,
                HighPriority = requirements.Count(r => r.Priority == "High" || r.Priority == "Highest"),
                MediumPriority = requirements.Count(r => r.Priority == "Medium"),
                LowPriority = requirements.Count(r => r.Priority == "Low" || r.Priority == "Lowest")
            }
        };

        return Ok(srs);
    }

    /// <summary>
    /// Generate Task Assignment Report
    /// </summary>
    [HttpGet("task-assignment/{groupId}")]
    public async Task<IActionResult> GenerateTaskAssignmentReport(int groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Tasks.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.Assignee)
            .Include(g => g.Members.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

        if (group == null) return NotFound("Group not found");

        var tasks = group.Tasks.ToList();
        var members = group.Members.ToList();

        var report = new
        {
            ReportTitle = $"Task Assignment Report - {group.Name}",
            GeneratedAt = DateTime.Now,
            Summary = new
            {
                TotalTasks = tasks.Count,
                AssignedTasks = tasks.Count(t => t.AssigneeId != null),
                UnassignedTasks = tasks.Count(t => t.AssigneeId == null),
                CompletedTasks = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.Done),
                InProgressTasks = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.InProgress),
                TodoTasks = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.Todo)
            },
            TasksByStatus = tasks.GroupBy(t => t.Status.ToString()).Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Tasks = g.Select(t => new
                {
                    t.Title,
                    AssigneeName = t.Assignee?.FullName ?? "Unassigned",
                    t.DueDate,
                    t.Priority
                })
            }),
            MemberWorkload = members.Select(m => new
            {
                m.FullName,
                m.StudentCode,
                TotalTasks = tasks.Count(t => t.AssigneeId == m.Id),
                Completed = tasks.Count(t => t.AssigneeId == m.Id && t.Status == Domain.Enums.TaskStatus.Done),
                InProgress = tasks.Count(t => t.AssigneeId == m.Id && t.Status == Domain.Enums.TaskStatus.InProgress),
                Todo = tasks.Count(t => t.AssigneeId == m.Id && t.Status == Domain.Enums.TaskStatus.Todo),
                CompletionRate = tasks.Any(t => t.AssigneeId == m.Id) 
                    ? Math.Round((double)tasks.Count(t => t.AssigneeId == m.Id && t.Status == Domain.Enums.TaskStatus.Done) / 
                                 tasks.Count(t => t.AssigneeId == m.Id) * 100, 1)
                    : 0
            }).OrderByDescending(m => m.TotalTasks)
        };

        return Ok(report);
    }

    /// <summary>
    /// Generate GitHub Commits Statistics Report
    /// </summary>
    [HttpGet("github-stats/{groupId}")]
    public async Task<IActionResult> GenerateGitHubStatsReport(int groupId)
    {
        var group = await _context.Groups
            .Include(g => g.GitHubCommits)
            .Include(g => g.Members.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

        if (group == null) return NotFound("Group not found");

        var commits = group.GitHubCommits.ToList();
        var members = group.Members.ToList();

        // Calculate commit statistics by author
        var commitsByAuthor = commits
            .GroupBy(c => c.AuthorName)
            .Select(g => new
            {
                AuthorName = g.Key,
                CommitCount = g.Count(),
                TotalAdditions = g.Sum(c => c.Additions),
                TotalDeletions = g.Sum(c => c.Deletions),
                FirstCommit = g.Min(c => c.CommitDate),
                LastCommit = g.Max(c => c.CommitDate)
            })
            .OrderByDescending(a => a.CommitCount)
            .ToList();

        // Calculate commits by date (last 30 days)
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        var commitsByDate = commits
            .Where(c => c.CommitDate >= thirtyDaysAgo)
            .GroupBy(c => c.CommitDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        var report = new
        {
            ReportTitle = $"GitHub Commits Statistics - {group.Name}",
            GeneratedAt = DateTime.Now,
            Repository = group.GitHubRepoUrl,
            Summary = new
            {
                TotalCommits = commits.Count,
                TotalContributors = commitsByAuthor.Count,
                TotalAdditions = commits.Sum(c => c.Additions),
                TotalDeletions = commits.Sum(c => c.Deletions),
                DateRange = commits.Any() ? new
                {
                    From = commits.Min(c => c.CommitDate),
                    To = commits.Max(c => c.CommitDate)
                } : null
            },
            ContributorStats = commitsByAuthor,
            CommitActivity = commitsByDate,
            RecentCommits = commits.OrderByDescending(c => c.CommitDate).Take(10).Select(c => new
            {
                c.CommitSha,
                c.Message,
                c.AuthorName,
                c.CommitDate,
                c.Additions,
                c.Deletions
            })
        };

        return Ok(report);
    }
}
