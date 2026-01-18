namespace SWP391.Domain.Entities;

/// <summary>
/// Requirement entity - synced from Jira Epics/Stories
/// </summary>
public class Requirement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? JiraIssueKey { get; set; }
    public string? JiraIssueUrl { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    
    // Navigation properties
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
    
    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
}
