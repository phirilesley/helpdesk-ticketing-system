using System;
using System.Collections.Generic;

namespace HelpDeskSystem.DevOps.Services
{
    public class DevOpsSettings
    {
        public string? GitHubToken { get; set; }
        public string? GitHubUrl { get; set; } = "https://api.github.com";
        public string? GitLabToken { get; set; }
        public string? GitLabUrl { get; set; }
        public string? AzureDevOpsToken { get; set; }
        public string? JenkinsUrl { get; set; }
    }

    public class GitRepository
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? FullName { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? CloneUrl { get; set; }
        public string? DefaultBranch { get; set; }
        public string? Language { get; set; }
        public int Stars { get; set; }
        public int Forks { get; set; }
        public bool IsPrivate { get; set; }
        public string? Provider { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class GitCommit
    {
        public string? Sha { get; set; }
        public string? Message { get; set; }
        public string? Author { get; set; }
        public string? AuthorEmail { get; set; }
        public DateTime Date { get; set; }
        public string? Url { get; set; }
        public int Added { get; set; }
        public int Removed { get; set; }
        public int Modified { get; set; }
    }

    public class GitBranch
    {
        public string? Name { get; set; }
        public string? CommitSha { get; set; }
        public bool Protected { get; set; }
        public bool Default { get; set; }
    }

    public class GitPullRequest
    {
        public string? Id { get; set; }
        public int Number { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? State { get; set; }
        public string? Author { get; set; }
        public string? SourceBranch { get; set; }
        public string? TargetBranch { get; set; }
        public string? Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool? Mergeable { get; set; }
        public bool Merged { get; set; }
        public List<string> Reviewers { get; set; } = new();
        public List<string> Assignees { get; set; } = new();
    }

    public class CIPipeline
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Provider { get; set; }
        public string? Status { get; set; }
        public long? LastRun { get; set; }
        public string? Url { get; set; }
        public bool IsRunning { get; set; }
    }

    public class CIBuild
    {
        public string? Id { get; set; }
        public int Number { get; set; }
        public string? Status { get; set; }
        public long Duration { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public string? Url { get; set; }
        public List<string> Artifacts { get; set; } = new();
    }

    public class CIDeployment
    {
        public string? Id { get; set; }
        public string? Environment { get; set; }
        public string? Status { get; set; }
        public DateTime DeployedAt { get; set; }
        public string? DeployedBy { get; set; }
        public string? Version { get; set; }
        public List<string> Artifacts { get; set; } = new();
    }

    public class CodeReview
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? State { get; set; }
        public string? Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Reviewers { get; set; } = new();
        public int Approvals { get; set; }
        public int Changes { get; set; }
        public int Comments { get; set; }
    }

    public class Deployment
    {
        public string? Id { get; set; }
        public string? Environment { get; set; }
        public string? Status { get; set; }
        public DateTime DeployedAt { get; set; }
        public string? Version { get; set; }
        public string? DeployedBy { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Features { get; set; } = new();
    }

    public enum DeploymentStatus { Pending, InProgress, Success, Failed, RolledBack }

    public class RollbackPlan
    {
        public string? DeploymentId { get; set; }
        public List<RollbackStep> RollbackSteps { get; set; } = new();
        public TimeSpan EstimatedTotalTime { get; set; }
        public string? RiskLevel { get; set; }
    }

    public class RollbackStep
    {
        public int Order { get; set; }
        public string? Description { get; set; }
        public TimeSpan EstimatedTime { get; set; }
    }

    public class FeatureBranch
    {
        public string? Name { get; set; }
        public string? TicketId { get; set; }
        public string? FeatureName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
    }

    public class SprintDeployment
    {
        public string? SprintId { get; set; }
        public DateTime PlannedDate { get; set; }
        public List<string> Features { get; set; } = new();
        public string? RiskAssessment { get; set; }
        public RollbackPlan? RollbackPlan { get; set; }
    }

    public class CodeQualityReport
    {
        public string? RepositoryId { get; set; }
        public string? Branch { get; set; }
        public double Coverage { get; set; }
        public int CodeSmells { get; set; }
        public int Vulnerabilities { get; set; }
        public string? TechnicalDebt { get; set; }
        public string? MaintainabilityRating { get; set; }
        public string? ReliabilityRating { get; set; }
        public string? SecurityRating { get; set; }
        public int DuplicatedLines { get; set; }
        public int LinesOfCode { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SecurityScanResult
    {
        public string? RepositoryId { get; set; }
        public DateTime ScanDate { get; set; }
        public int CriticalVulnerabilities { get; set; }
        public int HighVulnerabilities { get; set; }
        public int MediumVulnerabilities { get; set; }
        public int LowVulnerabilities { get; set; }
        public int TotalVulnerabilities { get; set; }
        public TimeSpan ScanDuration { get; set; }
        public List<string> Tools { get; set; } = new();
    }

    public class TestResult
    {
        public string? TestName { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int Total { get; set; }
        public TimeSpan Duration { get; set; }
        public double Coverage { get; set; }
    }

    // JSON Mapping classes
    public class GitHubRepository
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? FullName { get; set; }
        public string? Description { get; set; }
        public string? HtmlUrl { get; set; }
        public string? CloneUrl { get; set; }
        public string? DefaultBranch { get; set; }
        public string? Language { get; set; }
        public int StargazersCount { get; set; }
        public int ForksCount { get; set; }
        public bool Private { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class GitLabProject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? PathWithNamespace { get; set; }
        public string? Description { get; set; }
        public string? WebUrl { get; set; }
        public string? HttpUrlToRepo { get; set; }
        public string? DefaultBranch { get; set; }
        public List<string>? TagList { get; set; }
        public int StarCount { get; set; }
        public int ForksCount { get; set; }
        public string? Visibility { get; set; }
        public DateTime LastActivityAt { get; set; }
    }

    public class GitHubCommit
    {
        public string? Sha { get; set; }
        public GitHubCommitDetail Commit { get; set; } = new();
        public string? HtmlUrl { get; set; }
        public GitHubStats? Stats { get; set; }
    }

    public class GitHubCommitDetail
    {
        public string? Message { get; set; }
        public GitHubAuthor Author { get; set; } = new();
    }

    public class GitHubAuthor
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public DateTime Date { get; set; }
    }

    public class GitHubStats
    {
        public int Total { get; set; }
        public int Added { get; set; }
        public int Removed { get; set; }
        public int Modified { get; set; }
    }

    public class GitHubBranch
    {
        public string? Name { get; set; }
        public GitHubCommitSummary Commit { get; set; } = new();
        public bool Protected { get; set; }
    }

    public class GitHubCommitSummary
    {
        public string? Sha { get; set; }
    }

    public class GitHubPullRequest
    {
        public long Id { get; set; }
        public int Number { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? State { get; set; }
        public GitHubUser User { get; set; } = new();
        public GitHubBranchRef Head { get; set; } = new();
        public GitHubBranchRef Base { get; set; } = new();
        public string? HtmlUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool? Mergeable { get; set; }
        public bool Merged { get; set; }
        public List<GitHubUser>? RequestedReviewers { get; set; }
        public List<GitHubUser>? Assignees { get; set; }
        public int? ChangedFiles { get; set; }
        public int? Comments { get; set; }
    }

    public class GitHubUser
    {
        public string? Login { get; set; }
    }

    public class GitHubBranchRef
    {
        public string? Ref { get; set; }
    }

    public class JenkinsResponse
    {
        public List<JenkinsJob> Jobs { get; set; } = new();
    }

    public class JenkinsJob
    {
        public string? Name { get; set; }
        public JenkinsBuild? LastBuild { get; set; }
    }

    public class JenkinsBuild
    {
        public string? Id { get; set; }
        public int Number { get; set; }
        public string? Result { get; set; }
        public string? Url { get; set; }
        public long Timestamp { get; set; }
        public long Duration { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public List<JenkinsArtifact>? Artifacts { get; set; }
    }

    public class JenkinsArtifact
    {
        public string? FileName { get; set; }
    }

    public class GitHubStatus
    {
        public string? State { get; set; }
        public string? TargetUrl { get; set; }
        public string? Description { get; set; }
        public string? Context { get; set; }
    }

    public class GitHubWorkflowRun
    {
        public long Id { get; set; }
        public string? Status { get; set; }
        public string? Conclusion { get; set; }
        public string? HtmlUrl { get; set; }
    }

    public class GitHubDeployment
    {
        public long Id { get; set; }
        public string? Sha { get; set; }
        public string? @ref { get; set; }
        public string? Task { get; set; }
        public string? Environment { get; set; }
        public string? Description { get; set; }
    }

    public class GitHubDeploymentRequest
    {
        public string? Ref { get; set; }
        public string? Task { get; set; }
        public bool AutoMerge { get; set; }
        public string? Environment { get; set; }
        public string? Description { get; set; }
        public List<string>? RequiredContexts { get; set; }
        public object? Payload { get; set; }
    }

    public class GitHubRelease
    {
        public long Id { get; set; }
        public string? TagName { get; set; }
        public string? TargetCommitish { get; set; }
        public string? Name { get; set; }
        public string? Body { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? HtmlUrl { get; set; }
    }

    public class GitHubReleaseRequest
    {
        public string? TagName { get; set; }
        public string? TargetCommitish { get; set; }
        public string? Name { get; set; }
        public string? Body { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
    }
}
