using TaskStatusEnum = SWP391.Domain.Enums.TaskStatus;

namespace SWP391.Domain.Entities;

/// <summary>
/// ProjectTask entity - represents tasks assigned to team members
/// </summary>
public class ProjectTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Todo;
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? EstimatedHours { get; set; }
    public int? ActualHours { get; set; }
    
    // Jira sync
    public string? JiraIssueKey { get; set; }
    public string? JiraIssueUrl { get; set; }
    
    // Navigation properties
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
    
    public int? RequirementId { get; set; }
    public Requirement? Requirement { get; set; }
    
    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }
}
