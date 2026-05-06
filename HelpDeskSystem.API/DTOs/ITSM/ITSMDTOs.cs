using System;
using System.Collections.Generic;

namespace HelpDeskSystem.API.DTOs.ITSM
{
    public class CreateIncidentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public string Impact { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public string ReportedByUserId { get; set; } = string.Empty;
    }

    public class UpdateIncidentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public string Impact { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
    }

    public class IncidentFilterDto
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class AssignIncidentDto
    {
        public string AssigneeId { get; set; } = string.Empty;
        public string? AssignmentGroup { get; set; }
    }

    public class EscalateIncidentDto
    {
        public string Reason { get; set; } = string.Empty;
        public string EscalationLevel { get; set; } = string.Empty;
    }

    public class ResolveIncidentDto
    {
        public string Resolution { get; set; } = string.Empty;
        public string ResolutionCode { get; set; } = string.Empty;
    }

    public class CloseIncidentDto
    {
        public string ClosureCode { get; set; } = string.Empty;
        public string? SatisfactionRating { get; set; }
    }

    public class CreateProblemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
    }

    public class UpdateProblemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
    }

    public class ProblemFilterDto
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class LinkIncidentDto
    {
        public int IncidentId { get; set; }
    }

    public class PerformRCADto
    {
        public string AnalysisMethod { get; set; } = string.Empty;
        public string RootCause { get; set; } = string.Empty;
        public string ContributingFactors { get; set; } = string.Empty;
        public string Recommendations { get; set; } = string.Empty;
    }

    public class ImplementPermanentFixDto
    {
        public string FixDescription { get; set; } = string.Empty;
        public DateTime ImplementationDate { get; set; }
    }

    public class CreateChangeDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public string RiskAssessment { get; set; } = string.Empty;
        public string ImpactAssessment { get; set; } = string.Empty;
    }

    public class UpdateChangeDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public string RiskAssessment { get; set; } = string.Empty;
        public string ImpactAssessment { get; set; } = string.Empty;
    }

    public class ChangeFilterDto
    {
        public string? Status { get; set; }
        public string? Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class SubmitApprovalDto
    {
        public List<string> Approvers { get; set; } = new();
    }

    public class ApproveChangeDto
    {
        public string ApproverId { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }

    public class RejectChangeDto
    {
        public string ApproverId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class ScheduleChangeDto
    {
        public DateTime ScheduledDate { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
    }

    public class ImplementChangeDto
    {
        public string ImplementationDetails { get; set; } = string.Empty;
    }

    public class ReviewChangeDto
    {
        public string ReviewerId { get; set; } = string.Empty;
        public bool Successful { get; set; }
        public string? Comments { get; set; }
        public string? Issues { get; set; }
    }

    public class CreateCIDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CIType { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    public class UpdateCIDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CIType { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    public class CIFilterDto
    {
        public string? CIType { get; set; }
        public string? Status { get; set; }
        public string? Owner { get; set; }
        public string? Location { get; set; }
    }

    public class LinkCIDto
    {
        public int SourceCiId { get; set; }
        public int TargetCiId { get; set; }
        public string RelationshipType { get; set; } = string.Empty;
    }

    public class AssetLifecycleDto
    {
        public string LifecycleEvent { get; set; } = string.Empty;
    }

    public class CreateServiceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class ServiceFilterDto
    {
        public string? Category { get; set; }
        public string? Status { get; set; }
        public decimal? MinCost { get; set; }
        public decimal? MaxCost { get; set; }
    }

    public class CreateSLADto
    {
        public string Name { get; set; } = string.Empty;
        public int ServiceId { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public TimeSpan ResolutionTime { get; set; }
        public double AvailabilityPercentage { get; set; }
    }

    public class SLAFilterDto
    {
        public int? ServiceId { get; set; }
        public string? Status { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }

    public class SLABreachDto
    {
        public string BreachDetails { get; set; } = string.Empty;
        public DateTime BreachTime { get; set; }
    }

    public class CreateKnowledgeArticleDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    public class KnowledgeFilterDto
    {
        public string? Category { get; set; }
        public string? Status { get; set; }
        public List<string>? Tags { get; set; }
        public string? Author { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CreateProcessInstanceDto
    {
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class ComplianceReportRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ConductAuditDto
    {
        public List<string> Processes { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CertificationRequestDto
    {
        public string Level { get; set; } = string.Empty;
    }
}
