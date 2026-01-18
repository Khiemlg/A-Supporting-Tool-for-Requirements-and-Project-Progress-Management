namespace SWP391.Domain.Entities;

/// <summary>
/// GitHubCommit entity - stores commit statistics from GitHub
/// </summary>
public class GitHubCommit : BaseEntity
{
    public string CommitSha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public DateTime CommitDate { get; set; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public string? Url { get; set; }
    
    // Navigation properties
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
    
    public int? UserId { get; set; }
    public User? User { get; set; }
}
