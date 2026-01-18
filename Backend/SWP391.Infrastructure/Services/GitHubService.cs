using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SWP391.Application.Interfaces;
using SWP391.Domain.Entities;
using SWP391.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SWP391.Infrastructure.Services;

public class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ILogger<GitHubService> _logger;
    private readonly string? _token;

    public GitHubService(
        HttpClient httpClient,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<GitHubService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
        _token = configuration["GitHub:PersonalAccessToken"];

        _httpClient.BaseAddress = new Uri("https://api.github.com/");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SWP391-App", "1.0"));
        
        if (!string.IsNullOrEmpty(_token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }
    }

    public async Task<IEnumerable<GitHubCommitDto>> GetRepositoryCommitsAsync(string repoOwner, string repoName, int maxCommits = 100)
    {
        try
        {
            var commits = new List<GitHubCommitDto>();
            var page = 1;
            var perPage = Math.Min(maxCommits, 100);

            while (commits.Count < maxCommits)
            {
                var response = await _httpClient.GetAsync($"repos/{repoOwner}/{repoName}/commits?per_page={perPage}&page={page}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch commits: {StatusCode}", response.StatusCode);
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                var commitData = JsonSerializer.Deserialize<JsonElement[]>(json);

                if (commitData == null || commitData.Length == 0) break;

                foreach (var commit in commitData)
                {
                    if (commits.Count >= maxCommits) break;

                    var commitInfo = commit.GetProperty("commit");
                    var author = commitInfo.GetProperty("author");
                    
                    commits.Add(new GitHubCommitDto(
                        Sha: commit.GetProperty("sha").GetString() ?? "",
                        Message: commitInfo.GetProperty("message").GetString() ?? "",
                        AuthorName: author.GetProperty("name").GetString() ?? "",
                        AuthorEmail: author.GetProperty("email").GetString() ?? "",
                        CommittedAt: author.GetProperty("date").GetDateTime(),
                        Additions: 0, // Need separate API call for stats
                        Deletions: 0,
                        Url: commit.GetProperty("html_url").GetString() ?? ""
                    ));
                }

                page++;
            }

            return commits;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching commits for {Owner}/{Repo}", repoOwner, repoName);
            return Enumerable.Empty<GitHubCommitDto>();
        }
    }

    public async Task<IEnumerable<GitHubContributorDto>> GetContributorsAsync(string repoOwner, string repoName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"repos/{repoOwner}/{repoName}/contributors");
            
            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<GitHubContributorDto>();

            var json = await response.Content.ReadAsStringAsync();
            var contributors = JsonSerializer.Deserialize<JsonElement[]>(json);

            return contributors?.Select(c => new GitHubContributorDto(
                Login: c.GetProperty("login").GetString() ?? "",
                AvatarUrl: c.GetProperty("avatar_url").GetString() ?? "",
                Contributions: c.GetProperty("contributions").GetInt32()
            )) ?? Enumerable.Empty<GitHubContributorDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching contributors");
            return Enumerable.Empty<GitHubContributorDto>();
        }
    }

    public async Task<GitHubRepoStatsDto> GetRepositoryStatsAsync(string repoOwner, string repoName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"repos/{repoOwner}/{repoName}");
            
            if (!response.IsSuccessStatusCode)
                return new GitHubRepoStatsDto(0, 0, 0, 0, 0);

            var json = await response.Content.ReadAsStringAsync();
            var repo = JsonSerializer.Deserialize<JsonElement>(json);

            return new GitHubRepoStatsDto(
                TotalCommits: 0, // Need separate API call
                TotalContributors: 0,
                OpenIssues: repo.GetProperty("open_issues_count").GetInt32(),
                Stars: repo.GetProperty("stargazers_count").GetInt32(),
                Forks: repo.GetProperty("forks_count").GetInt32()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching repo stats");
            return new GitHubRepoStatsDto(0, 0, 0, 0, 0);
        }
    }

    public async Task SyncCommitsToGroupAsync(int groupId, string repoOwner, string repoName)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) return;

        var commits = await GetRepositoryCommitsAsync(repoOwner, repoName);
        
        foreach (var commit in commits)
        {
            // Check if commit already exists
            var exists = await _context.GitHubCommits.AnyAsync(c => c.CommitSha == commit.Sha);
            if (exists) continue;

            // Try to match author to existing user
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.Email == commit.AuthorEmail || u.GitHubUsername == commit.AuthorName);

            var gitHubCommit = new GitHubCommit
            {
                CommitSha = commit.Sha,
                Message = commit.Message.Length > 500 ? commit.Message.Substring(0, 500) : commit.Message,
                AuthorName = commit.AuthorName,
                AuthorEmail = commit.AuthorEmail,
                CommitDate = commit.CommittedAt,
                Additions = commit.Additions,
                Deletions = commit.Deletions,
                Url = commit.Url,
                GroupId = groupId,
                UserId = user?.Id
            };

            _context.GitHubCommits.Add(gitHubCommit);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Synced {Count} commits for group {GroupId}", commits.Count(), groupId);
    }
}
