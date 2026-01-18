using SWP391.Domain.Enums;

namespace SWP391.Domain.Entities;

/// <summary>
/// User entity - represents all users in the system
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? StudentCode { get; set; } // For students only
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; }
    
    // External integrations
    public string? JiraAccountId { get; set; }
    public string? GitHubUsername { get; set; }
    
    // Navigation properties
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    
    // For Team Leader
    public Group? LeadingGroup { get; set; }
    
    // For Lecturer
    public ICollection<Group> AssignedGroups { get; set; } = new List<Group>();
}
