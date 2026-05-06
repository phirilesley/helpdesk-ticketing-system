using System;
using System.Collections.Generic;

namespace HelpDeskSystem.API.DTOs.DevOps
{
    public class CreatePullRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Head { get; set; } = string.Empty;
        public string Base { get; set; } = string.Empty;
        public string SourceBranch => Head;
        public string TargetBranch => Base;
    }

    public class CreateBranchDto
    {
        public string BranchName { get; set; } = string.Empty;
        public string FromBranch { get; set; } = "main";
    }

    public class CreateCommitDto
    {
        public string Message { get; set; } = string.Empty;
        public string Tree { get; set; } = string.Empty;
        public string[] Parents { get; set; } = Array.Empty<string>();
    }

    public class CreateStatusDto
    {
        public string State { get; set; } = string.Empty;
        public string TargetUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
    }

    public class TriggerWorkflowDto
    {
        public Dictionary<string, object> Inputs { get; set; } = new();
    }

    public class CreateDeploymentDto
    {
        public string @ref { get; set; } = string.Empty;
        public string Ref => @ref;
        public string Task { get; set; } = "deploy";
        public bool AutoMerge { get; set; } = true;
        public string Environment { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredContexts { get; set; } = new();
    }

    public class CreateReleaseDto
    {
        public string TagName { get; set; } = string.Empty;
        public string TargetCommitish { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
    }

    public class CreateFeatureBranchDto
    {
        public string TicketId { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
    }

    public class MergeBranchDto
    {
        public string TargetBranch { get; set; } = "main";
    }

    public class TriggerPipelineDto
    {
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class CreateCodeReviewDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Reviewers { get; set; } = new();
    }

    public class RequestChangesDto
    {
        public string Comment { get; set; } = string.Empty;
    }

    public class TrackDeploymentDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class MergeFeatureBranchDto
    {
        public string TargetBranch { get; set; } = "main";
    }

    public class PlanSprintDeploymentDto
    {
        public DateTime? PlannedAt { get; set; }
    }

    public class DevOpsMetricsFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Team { get; set; }
        public string? RepositoryId { get; set; }
        public string? Environment { get; set; }
    }
}
