namespace SWP391.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern interface
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IGroupRepository Groups { get; }
    IRequirementRepository Requirements { get; }
    ITaskRepository Tasks { get; }
    IGitHubCommitRepository GitHubCommits { get; }
    
    Task<int> SaveChangesAsync();
}
