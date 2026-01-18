namespace SWP391.Application.DTOs.Admin;

// Group DTOs
public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? JiraProjectKey { get; set; }
    public string? GitHubRepoUrl { get; set; }
    public int? LeaderId { get; set; }
    public string? LeaderName { get; set; }
    public int? LecturerId { get; set; }
    public string? LecturerName { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? JiraProjectKey { get; set; }
    public string? GitHubRepoUrl { get; set; }
}

public class UpdateGroupDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? JiraProjectKey { get; set; }
    public string? GitHubRepoUrl { get; set; }
    public int? LeaderId { get; set; }
    public int? LecturerId { get; set; }
}

// User Management DTOs
public class UserListDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserRoleDto
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class AssignUserToGroupDto
{
    public int UserId { get; set; }
    public int? GroupId { get; set; }
}

public class AssignLecturerDto
{
    public int GroupId { get; set; }
    public int LecturerId { get; set; }
}
