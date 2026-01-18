-- =============================================
-- SWP391 Project Management Tool - Database Schema
-- SQL Server Script
-- Created: 2026-01-16
-- =============================================

-- Create Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SWP391_ProjectManagement')
BEGIN
    CREATE DATABASE SWP391_ProjectManagement;
END
GO

USE SWP391_ProjectManagement;
GO

-- =============================================
-- DROP EXISTING TABLES (for clean reinstall)
-- =============================================
IF OBJECT_ID('dbo.GitHubCommits', 'U') IS NOT NULL DROP TABLE dbo.GitHubCommits;
IF OBJECT_ID('dbo.ProjectTasks', 'U') IS NOT NULL DROP TABLE dbo.ProjectTasks;
IF OBJECT_ID('dbo.Requirements', 'U') IS NOT NULL DROP TABLE dbo.Requirements;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Groups', 'U') IS NOT NULL DROP TABLE dbo.Groups;
GO

-- =============================================
-- TABLE: Groups
-- =============================================
CREATE TABLE dbo.Groups (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    JiraProjectKey NVARCHAR(50) NULL,
    GitHubRepoUrl NVARCHAR(500) NULL,
    LeaderId INT NULL,
    LecturerId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

CREATE INDEX IX_Groups_Name ON dbo.Groups(Name);
CREATE INDEX IX_Groups_JiraProjectKey ON dbo.Groups(JiraProjectKey);
GO

-- =============================================
-- TABLE: Users
-- =============================================
CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    StudentCode NVARCHAR(20) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    AvatarUrl NVARCHAR(500) NULL,
    Role INT NOT NULL, -- 1: Admin, 2: Lecturer, 3: TeamLeader, 4: TeamMember
    JiraAccountId NVARCHAR(100) NULL,
    GitHubUsername NVARCHAR(100) NULL,
    GroupId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

CREATE INDEX IX_Users_Email ON dbo.Users(Email);
CREATE INDEX IX_Users_Role ON dbo.Users(Role);
CREATE INDEX IX_Users_GroupId ON dbo.Users(GroupId);
CREATE INDEX IX_Users_GitHubUsername ON dbo.Users(GitHubUsername);
GO

-- Add FK for Users.GroupId
ALTER TABLE dbo.Users
    ADD CONSTRAINT FK_Users_Groups FOREIGN KEY (GroupId) REFERENCES dbo.Groups(Id) ON DELETE SET NULL;
GO

-- Add FK for Groups.LeaderId and Groups.LecturerId
ALTER TABLE dbo.Groups
    ADD CONSTRAINT FK_Groups_Leader FOREIGN KEY (LeaderId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION;

ALTER TABLE dbo.Groups
    ADD CONSTRAINT FK_Groups_Lecturer FOREIGN KEY (LecturerId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION;
GO

-- =============================================
-- TABLE: Requirements (synced from Jira)
-- =============================================
CREATE TABLE dbo.Requirements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    JiraIssueKey NVARCHAR(50) NULL,
    JiraIssueUrl NVARCHAR(500) NULL,
    Priority NVARCHAR(50) NULL,
    Status NVARCHAR(50) NULL,
    GroupId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_Requirements_Groups FOREIGN KEY (GroupId) REFERENCES dbo.Groups(Id) ON DELETE NO ACTION
);
GO

CREATE INDEX IX_Requirements_GroupId ON dbo.Requirements(GroupId);
CREATE INDEX IX_Requirements_JiraIssueKey ON dbo.Requirements(JiraIssueKey);
GO

-- =============================================
-- TABLE: ProjectTasks
-- =============================================
CREATE TABLE dbo.ProjectTasks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Status INT NOT NULL DEFAULT 1, -- 1: Todo, 2: InProgress, 3: Review, 4: Done
    Priority NVARCHAR(50) NULL,
    DueDate DATETIME2 NULL,
    EstimatedHours INT NULL,
    ActualHours INT NULL,
    JiraIssueKey NVARCHAR(50) NULL,
    JiraIssueUrl NVARCHAR(500) NULL,
    GroupId INT NOT NULL,
    RequirementId INT NULL,
    AssigneeId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_ProjectTasks_Groups FOREIGN KEY (GroupId) REFERENCES dbo.Groups(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_ProjectTasks_Requirements FOREIGN KEY (RequirementId) REFERENCES dbo.Requirements(Id) ON DELETE SET NULL,
    CONSTRAINT FK_ProjectTasks_Users FOREIGN KEY (AssigneeId) REFERENCES dbo.Users(Id) ON DELETE SET NULL
);
GO

CREATE INDEX IX_ProjectTasks_GroupId ON dbo.ProjectTasks(GroupId);
CREATE INDEX IX_ProjectTasks_AssigneeId ON dbo.ProjectTasks(AssigneeId);
CREATE INDEX IX_ProjectTasks_Status ON dbo.ProjectTasks(Status);
CREATE INDEX IX_ProjectTasks_JiraIssueKey ON dbo.ProjectTasks(JiraIssueKey);
GO

-- =============================================
-- TABLE: GitHubCommits
-- =============================================
CREATE TABLE dbo.GitHubCommits (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CommitSha NVARCHAR(40) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    AuthorName NVARCHAR(100) NOT NULL,
    AuthorEmail NVARCHAR(256) NOT NULL,
    CommitDate DATETIME2 NOT NULL,
    Additions INT NOT NULL DEFAULT 0,
    Deletions INT NOT NULL DEFAULT 0,
    Url NVARCHAR(500) NULL,
    GroupId INT NOT NULL,
    UserId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT UQ_GitHubCommits_Sha UNIQUE (CommitSha),
    CONSTRAINT FK_GitHubCommits_Groups FOREIGN KEY (GroupId) REFERENCES dbo.Groups(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_GitHubCommits_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL
);
GO

CREATE INDEX IX_GitHubCommits_GroupId ON dbo.GitHubCommits(GroupId);
CREATE INDEX IX_GitHubCommits_UserId ON dbo.GitHubCommits(UserId);
CREATE INDEX IX_GitHubCommits_CommitDate ON dbo.GitHubCommits(CommitDate);
GO

-- =============================================
-- INSERT SAMPLE DATA
-- =============================================

-- Insert Admin user (password: Admin@123 - you need to hash this properly in app)
INSERT INTO dbo.Users (Email, PasswordHash, FullName, Role)
VALUES ('admin@fpt.edu.vn', 'TEMP_HASH_REPLACE_IN_APP', N'System Admin', 1);

-- Insert sample Lecturer
INSERT INTO dbo.Users (Email, PasswordHash, FullName, Role)
VALUES ('lecturer@fpt.edu.vn', 'TEMP_HASH_REPLACE_IN_APP', N'Nguyen Van A', 2);

PRINT 'Database schema created successfully!';
GO
