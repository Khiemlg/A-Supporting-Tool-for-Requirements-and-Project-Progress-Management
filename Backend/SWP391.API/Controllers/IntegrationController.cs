using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Application.Interfaces;
using SWP391.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SWP391.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntegrationController : ControllerBase
{
    private readonly IGitHubService _gitHubService;
    private readonly IJiraService _jiraService;
    private readonly AppDbContext _context;

    public IntegrationController(
        IGitHubService gitHubService,
        IJiraService jiraService,
        AppDbContext context)
    {
        _gitHubService = gitHubService;
        _jiraService = jiraService;
        _context = context;
    }

    /// <summary>
    /// Sync GitHub commits for a group
    /// </summary>
    [HttpPost("github/sync/{groupId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> SyncGitHubCommits(int groupId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) return NotFound("Group not found");
        
        if (string.IsNullOrEmpty(group.GitHubRepoUrl))
            return BadRequest("Group has no GitHub repo configured");

        // Parse owner and repo from URL
        var (owner, repo) = ParseGitHubUrl(group.GitHubRepoUrl);
        if (owner == null || repo == null)
            return BadRequest("Invalid GitHub URL format");

        await _gitHubService.SyncCommitsToGroupAsync(groupId, owner, repo);
        return Ok(new { Message = "GitHub commits synced successfully" });
    }

    /// <summary>
    /// Get GitHub commits for a group
    /// </summary>
    [HttpGet("github/commits/{groupId}")]
    public async Task<IActionResult> GetGitHubCommits(int groupId)
    {
        var commits = await _context.GitHubCommits
            .Where(c => c.GroupId == groupId)
            .OrderByDescending(c => c.CommitDate)
            .Take(50)
            .Select(c => new
            {
                c.Id,
                c.CommitSha,
                c.Message,
                c.AuthorName,
                c.CommitDate,
                c.Additions,
                c.Deletions,
                c.Url
            })
            .ToListAsync();

        return Ok(commits);
    }

    /// <summary>
    /// Sync Jira issues for a group
    /// </summary>
    [HttpPost("jira/sync/{groupId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> SyncJiraIssues(int groupId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) return NotFound("Group not found");
        
        if (string.IsNullOrEmpty(group.JiraProjectKey))
            return BadRequest("Group has no Jira project configured");

        await _jiraService.SyncIssuesToGroupAsync(groupId, group.JiraProjectKey);
        return Ok(new { Message = "Jira issues synced successfully" });
    }

    /// <summary>
    /// Get repository stats
    /// </summary>
    [HttpGet("github/stats")]
    public async Task<IActionResult> GetGitHubStats([FromQuery] string repoUrl)
    {
        var (owner, repo) = ParseGitHubUrl(repoUrl);
        if (owner == null || repo == null)
            return BadRequest("Invalid GitHub URL");

        var stats = await _gitHubService.GetRepositoryStatsAsync(owner, repo);
        var contributors = await _gitHubService.GetContributorsAsync(owner, repo);

        return Ok(new { Stats = stats, Contributors = contributors });
    }

    /// <summary>
    /// Get GitHub contributors
    /// </summary>
    [HttpGet("github/contributors")]
    public async Task<IActionResult> GetContributors([FromQuery] string repoUrl)
    {
        var (owner, repo) = ParseGitHubUrl(repoUrl);
        if (owner == null || repo == null)
            return BadRequest("Invalid GitHub URL");

        var contributors = await _gitHubService.GetContributorsAsync(owner, repo);
        return Ok(contributors);
    }

    private (string? Owner, string? Repo) ParseGitHubUrl(string url)
    {
        try
        {
            // Handle formats: https://github.com/owner/repo or https://github.com/owner/repo.git
            var uri = new Uri(url.Replace(".git", ""));
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            
            if (parts.Length >= 2)
                return (parts[0], parts[1]);
            
            return (null, null);
        }
        catch
        {
            return (null, null);
        }
    }
}
