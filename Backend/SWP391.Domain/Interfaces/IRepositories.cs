using SWP391.Domain.Entities;

namespace SWP391.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetByGroupIdAsync(int groupId);
    Task<IEnumerable<User>> GetLecturersAsync();
    Task<User?> GetByGitHubUsernameAsync(string username);
}

public interface IGroupRepository : IRepository<Group>
{
    Task<IEnumerable<Group>> GetByLecturerIdAsync(int lecturerId);
    Task<Group?> GetWithMembersAsync(int id);
    Task<Group?> GetByJiraProjectKeyAsync(string projectKey);
}

public interface IRequirementRepository : IRepository<Requirement>
{
    Task<IEnumerable<Requirement>> GetByGroupIdAsync(int groupId);
    Task<Requirement?> GetByJiraKeyAsync(string jiraKey);
}

public interface ITaskRepository : IRepository<ProjectTask>
{
    Task<IEnumerable<ProjectTask>> GetByGroupIdAsync(int groupId);
    Task<IEnumerable<ProjectTask>> GetByAssigneeIdAsync(int assigneeId);
    Task<IEnumerable<ProjectTask>> GetByRequirementIdAsync(int requirementId);
}

public interface IGitHubCommitRepository : IRepository<GitHubCommit>
{
    Task<IEnumerable<GitHubCommit>> GetByGroupIdAsync(int groupId);
    Task<IEnumerable<GitHubCommit>> GetByUserIdAsync(int userId);
    Task<GitHubCommit?> GetByShaAsync(string sha);
}
