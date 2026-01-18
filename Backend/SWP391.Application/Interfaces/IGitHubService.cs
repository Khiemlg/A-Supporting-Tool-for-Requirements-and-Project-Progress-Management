namespace SWP391.Application.Interfaces;

/// <summary>
/// Interface for GitHub API integration service
/// </summary>
public interface IGitHubService
{
    Task<IEnumerable<GitHubCommitDto>> GetRepositoryCommitsAsync(string repoOwner, string repoName, int maxCommits = 100);
    Task<IEnumerable<GitHubContributorDto>> GetContributorsAsync(string repoOwner, string repoName);
    Task<GitHubRepoStatsDto> GetRepositoryStatsAsync(string repoOwner, string repoName);
    Task SyncCommitsToGroupAsync(int groupId, string repoOwner, string repoName);
}

public record GitHubCommitDto(
    string Sha,
    string Message,
    string AuthorName,
    string AuthorEmail,
    DateTime CommittedAt,
    int Additions,
    int Deletions,
    string Url
);

public record GitHubContributorDto(
    string Login,
    string AvatarUrl,
    int Contributions
);

public record GitHubRepoStatsDto(
    int TotalCommits,
    int TotalContributors,
    int OpenIssues,
    int Stars,
    int Forks
);
