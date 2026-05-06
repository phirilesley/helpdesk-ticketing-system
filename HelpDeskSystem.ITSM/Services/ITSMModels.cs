using System;
using System.Collections.Generic;

namespace HelpDeskSystem.ITSM.Services
{
    public class ITSMSettings
    {
        public string? DefaultPriority { get; set; }
    }

    public class Incident
    {
        public int Id { get; set; }
        public string? IncidentNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public string? Impact { get; set; }
        public string? Urgency { get; set; }
        public IncidentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? AssignedToUserId { get; set; }
        public string? AssignmentGroup { get; set; }
        public string? ReportedByUserId { get; set; }
        public int TicketId { get; set; }
        public string? EscalationLevel { get; set; }
        public string? EscalationReason { get; set; }
        public DateTime? EscalatedAt { get; set; }
        public string? Resolution { get; set; }
        public string? ResolutionCode { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ClosureCode { get; set; }
        public string? SatisfactionRating { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public int? ProblemId { get; set; }
    }

    // IncidentStatus is defined in ITSMService.cs

    public class IncidentFilter
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class Problem
    {
        public int Id { get; set; }
        public string? ProblemNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public ProblemStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TicketId { get; set; }
        public List<int> RelatedIncidents { get; set; } = new();
    }

    // ProblemStatus is defined in ITSMService.cs

    public class ProblemFilter
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class RootCauseAnalysis
    {
        public int ProblemId { get; set; }
        public string? AnalysisMethod { get; set; }
        public string? RootCause { get; set; }
        public string? ContributingFactors { get; set; }
        public string? Recommendations { get; set; }
        public DateTime PerformedAt { get; set; }
        public RCAStatus Status { get; set; }
        public string? IncidentPatterns { get; set; }
    }

    public enum RCAStatus { InProgress, Completed, Draft }

    public class ChangeRequest
    {
        public int Id { get; set; }
        public string? ChangeNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public string? ChangeType { get; set; }
        public string? RiskAssessment { get; set; }
        public string? ImpactAssessment { get; set; }
        public ChangeStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TicketId { get; set; }
        public List<string> Approvers { get; set; } = new();
        public DateTime? SubmittedForApprovalAt { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public TimeSpan? EstimatedDuration { get; set; }
    }

    // ChangeStatus is defined in ITSMService.cs

    public class ChangeFilter
    {
        public string? Status { get; set; }
        public string? Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    // ChangeReview is defined in ITSMService.cs

    public class ConfigurationItem
    {
        public int Id { get; set; }
        public string? CINumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? CIType { get; set; }
        public string? Owner { get; set; }
        public string? Location { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new();
        public CIStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum CIStatus { Active, Inactive, Retired, InMaintenance }

    public class CIFilter
    {
        public string? CIType { get; set; }
        public string? Status { get; set; }
        public string? Owner { get; set; }
        public string? Location { get; set; }
    }

    public class AssetLifecycle { }
    public enum AssetLifecycleEvent { Purchased, Deployed, Repaired, Retired }

    public class ServiceCatalogItem
    {
        public int Id { get; set; }
        public string? ServiceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "USD";
        public ServiceStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public ServiceLevelAgreement? SLA { get; set; }
    }

    public enum ServiceStatus { Active, Inactive, Draft }

    public class ServiceFilter
    {
        public string? Category { get; set; }
        public string? Status { get; set; }
        public decimal? MinCost { get; set; }
        public decimal? MaxCost { get; set; }
    }

    public class ServiceRequest
    {
        public int Id { get; set; }
        public string? RequestNumber { get; set; }
    }

    public class ServiceRequestFilter { }

    public class ServiceLevelAgreement
    {
        public string? SLAId { get; set; }
        public string? Name { get; set; }
        public int ServiceId { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public TimeSpan ResolutionTime { get; set; }
        public double AvailabilityPercentage { get; set; }
        public SLAStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? BreachPenalties { get; set; }
    }

    // SLA and Knowledge Management Models - These are defined in ITSMService.cs
    // SLAStatus, SLAFilter, SLABreach, BreachStatus, SLAMetric are defined in ITSMService.cs

    public class KnowledgeArticle
    {
        public int Id { get; set; }
        public string? ArticleNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public ArticleStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public enum ArticleStatus { Draft, Published, Archived, Review }

    public class KnowledgeFilter
    {
        public string? Category { get; set; }
        public string? Status { get; set; }
        public List<string>? Tags { get; set; }
        public string? Author { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class KnowledgeFeedback { }

    public class SelfServiceRequest
    {
        public string? RequestNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public SelfServiceStatus Status { get; set; }
    }

    public enum SelfServiceStatus { New, PendingApproval, Fulfilling, Completed, Canceled }

    public class ServiceRequestTemplate { }
    public class CatalogItem { }

    public class ITILComplianceReport
    {
        public string? ReportPeriod { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Framework { get; set; } = "ITIL 4";
        public object? IncidentManagementCompliance { get; set; }
        public object? ProblemManagementCompliance { get; set; }
        public object? ChangeManagementCompliance { get; set; }
        public object? AssetManagementCompliance { get; set; }
        public double OverallComplianceScore { get; set; }
        public object? SLACompliance { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    // ITIL Compliance Service Models - These are defined in ITSMService.cs
    // ITILProcess, ITILAudit, ITILFramework, ITILProcessDefinition, ITILProcessData, ITILProcessInstance, ITILAuditScope are defined in ITSMService.cs
    public class ITILMaturityAssessment { }
    public class ITILCertificationPreparation { }
    public class ITSMAnalyticsFilter { }
    public class ITILMetricsFilter { }

    // Missing SLA Types
    public class SLAFilter
    {
        public string? ServiceId { get; set; }
        public string? Status { get; set; }
        public DateTime? ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
    }

    public class SLAMetric
    {
        public string MetricId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public double Value { get; set; }
        public double Target { get; set; }
        public DateTime MeasuredAt { get; set; }
    }

    public class BreachPenalty
    {
        public string PenaltyId { get; set; } = string.Empty;
        public string BreachId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Missing ITIL Types
    public class ITILProcess
    {
        public string ProcessId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ITILAudit
    {
        public string AuditId { get; set; } = string.Empty;
        public string ProcessId { get; set; } = string.Empty;
        public DateTime AuditDate { get; set; }
        public string Auditor { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public List<string> Findings { get; set; } = new();
        public ITILAuditScope? Scope { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public AuditStatus Status { get; set; }
        public object? IncidentManagementAudit { get; set; }
        public object? ProblemManagementAudit { get; set; }
        public object? ChangeManagementAudit { get; set; }
        public object? SLAComplianceAudit { get; set; }
        public double OverallScore { get; set; }
    }

    public enum AuditStatus
    {
        InProgress,
        Completed,
        Cancelled
    }
}
