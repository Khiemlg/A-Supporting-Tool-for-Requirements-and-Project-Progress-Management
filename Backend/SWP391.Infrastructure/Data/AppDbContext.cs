using Microsoft.EntityFrameworkCore;
using SWP391.Domain.Entities;

namespace SWP391.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();
    public DbSet<GitHubCommit> GitHubCommits => Set<GitHubCommit>();
    public DbSet<IntegrationSetting> IntegrationSettings => Set<IntegrationSetting>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StudentCode).HasMaxLength(20);
            
            // Soft delete filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            
            entity.HasOne(g => g.Leader)
                .WithOne(u => u.LeadingGroup)
                .HasForeignKey<Group>(g => g.LeaderId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(g => g.Lecturer)
                .WithMany(u => u.AssignedGroups)
                .HasForeignKey(g => g.LecturerId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasMany(g => g.Members)
                .WithOne(u => u.Group)
                .HasForeignKey(u => u.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        // Requirement configuration
        modelBuilder.Entity<Requirement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            
            entity.HasOne(r => r.Group)
                .WithMany(g => g.Requirements)
                .HasForeignKey(r => r.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        // ProjectTask configuration
        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            
            entity.HasOne(t => t.Group)
                .WithMany(g => g.Tasks)
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(t => t.Requirement)
                .WithMany(r => r.Tasks)
                .HasForeignKey(t => t.RequirementId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(t => t.Assignee)
                .WithMany()
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        // GitHubCommit configuration
        modelBuilder.Entity<GitHubCommit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CommitSha).IsUnique();
            entity.Property(e => e.CommitSha).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Message).IsRequired();
            
            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto update timestamps
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}
