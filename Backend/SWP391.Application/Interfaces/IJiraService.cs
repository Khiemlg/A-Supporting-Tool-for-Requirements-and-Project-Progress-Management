namespace SWP391.Application.Interfaces;

/// <summary>
/// Interface for Jira API integration service
/// </summary>
public interface IJiraService
{
    Task<IEnumerable<JiraIssueDto>> GetProjectIssuesAsync(string projectKey);
    Task<JiraIssueDto?> GetIssueAsync(string issueKey);
    Task<IEnumerable<JiraSprintDto>> GetSprintsAsync(string boardId);
    Task SyncIssuesToGroupAsync(int groupId, string projectKey);
}

public record JiraIssueDto(
    string Key,
    string Summary,
    string Description,
    string Status,
    string Priority,
    string IssueType,
    string? AssigneeName,
    string? AssigneeEmail,
    DateTime Created,
    DateTime? Updated,
    string Url
);

public record JiraSprintDto(
    int Id,
    string Name,
    string State,
    DateTime? StartDate,
    DateTime? EndDate
);
