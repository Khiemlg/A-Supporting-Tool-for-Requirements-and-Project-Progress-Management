using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SWP391.Application.Interfaces;
using SWP391.Domain.Entities;
using SWP391.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SWP391.Infrastructure.Services;

public class JiraService : IJiraService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ILogger<JiraService> _logger;
    private readonly string? _configBaseUrl;
    private readonly string? _configEmail;
    private readonly string? _configApiToken;

    public JiraService(
        HttpClient httpClient,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<JiraService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
        
        _configBaseUrl = configuration["Jira:BaseUrl"];
        _configEmail = configuration["Jira:Email"];
        _configApiToken = configuration["Jira:ApiToken"];

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task<(string? baseUrl, string? email, string? token)> GetCredentialsAsync()
    {
        var settings = await _context.IntegrationSettings
            .Where(s => s.Key.StartsWith("Jira:"))
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var baseUrl = settings.GetValueOrDefault("Jira:BaseUrl") ?? _configBaseUrl;
        var email = settings.GetValueOrDefault("Jira:Email") ?? _configEmail;
        var token = settings.GetValueOrDefault("Jira:ApiToken") ?? _configApiToken;

        return (baseUrl, email, token);
    }

    private async Task ConfigureAuthAsync()
    {
        var (baseUrl, email, token) = await GetCredentialsAsync();
        
        if (!string.IsNullOrEmpty(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(token))
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{email}:{token}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        }
    }

    public async Task<IEnumerable<JiraIssueDto>> GetProjectIssuesAsync(string projectKey)
    {
        try
        {
            await ConfigureAuthAsync();
            var jql = $"project={projectKey} ORDER BY created DESC";
            var response = await _httpClient.GetAsync($"/rest/api/3/search?jql={Uri.EscapeDataString(jql)}&maxResults=100");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Jira issues: {StatusCode}", response.StatusCode);
                return Enumerable.Empty<JiraIssueDto>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);
            var issues = result.GetProperty("issues");

            return issues.EnumerateArray().Select(issue => ParseIssue(issue, projectKey)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Jira issues for project {ProjectKey}", projectKey);
            return Enumerable.Empty<JiraIssueDto>();
        }
    }

    public async Task<JiraIssueDto?> GetIssueAsync(string issueKey)
    {
        try
        {
            await ConfigureAuthAsync();
            var response = await _httpClient.GetAsync($"/rest/api/3/issue/{issueKey}");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var issue = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseIssue(issue, issueKey.Split('-')[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Jira issue {IssueKey}", issueKey);
            return null;
        }
    }

    public async Task<IEnumerable<JiraSprintDto>> GetSprintsAsync(string boardId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/rest/agile/1.0/board/{boardId}/sprint");
            
            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<JiraSprintDto>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);
            var sprints = result.GetProperty("values");

            return sprints.EnumerateArray().Select(s => new JiraSprintDto(
                Id: s.GetProperty("id").GetInt32(),
                Name: s.GetProperty("name").GetString() ?? "",
                State: s.GetProperty("state").GetString() ?? "",
                StartDate: s.TryGetProperty("startDate", out var start) ? start.GetDateTime() : null,
                EndDate: s.TryGetProperty("endDate", out var end) ? end.GetDateTime() : null
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Jira sprints for board {BoardId}", boardId);
            return Enumerable.Empty<JiraSprintDto>();
        }
    }

    public async Task SyncIssuesToGroupAsync(int groupId, string projectKey)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) return;

        var issues = await GetProjectIssuesAsync(projectKey);
        
        foreach (var issue in issues)
        {
            // Check if requirement already exists
            var exists = await _context.Requirements.AnyAsync(r => r.JiraIssueKey == issue.Key);
            if (exists) continue;

            var requirement = new Requirement
            {
                Title = issue.Summary,
                Description = issue.Description,
                JiraIssueKey = issue.Key,
                JiraIssueUrl = issue.Url,
                Priority = issue.Priority,
                Status = issue.Status,
                GroupId = groupId
            };

            _context.Requirements.Add(requirement);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Synced {Count} issues for group {GroupId}", issues.Count(), groupId);
    }

    private JiraIssueDto ParseIssue(JsonElement issue, string projectKey)
    {
        var fields = issue.GetProperty("fields");
        
        string? assigneeName = null;
        string? assigneeEmail = null;
        if (fields.TryGetProperty("assignee", out var assignee) && assignee.ValueKind != JsonValueKind.Null)
        {
            assigneeName = assignee.GetProperty("displayName").GetString();
            assigneeEmail = assignee.GetProperty("emailAddress").GetString();
        }

        return new JiraIssueDto(
            Key: issue.GetProperty("key").GetString() ?? "",
            Summary: fields.GetProperty("summary").GetString() ?? "",
            Description: fields.TryGetProperty("description", out var desc) 
                ? desc.ValueKind == JsonValueKind.Object ? "[Rich text content]" : desc.GetString() ?? ""
                : "",
            Status: fields.GetProperty("status").GetProperty("name").GetString() ?? "",
            Priority: fields.TryGetProperty("priority", out var priority) && priority.ValueKind != JsonValueKind.Null
                ? priority.GetProperty("name").GetString() ?? ""
                : "Medium",
            IssueType: fields.GetProperty("issuetype").GetProperty("name").GetString() ?? "",
            AssigneeName: assigneeName,
            AssigneeEmail: assigneeEmail,
            Created: fields.GetProperty("created").GetDateTime(),
            Updated: fields.TryGetProperty("updated", out var updated) ? updated.GetDateTime() : null,
            Url: $"{_configBaseUrl}/browse/{issue.GetProperty("key").GetString()}"
        );
    }
}
