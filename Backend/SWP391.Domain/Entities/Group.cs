namespace SWP391.Domain.Entities;

/// <summary>
/// Group entity - represents student groups for SWP391 projects
/// </summary>
public class Group : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? JiraProjectKey { get; set; }
    public string? GitHubRepoUrl { get; set; }
    
    // Navigation properties
    public int? LeaderId { get; set; }
    public User? Leader { get; set; }
    
    public int? LecturerId { get; set; }
    public User? Lecturer { get; set; }
    
    public ICollection<User> Members { get; set; } = new List<User>();
    public ICollection<Requirement> Requirements { get; set; } = new List<Requirement>();
    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<GitHubCommit> GitHubCommits { get; set; } = new List<GitHubCommit>();
}
