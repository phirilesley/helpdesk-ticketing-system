using System;
using System.Collections.Generic;

namespace HelpDeskSystem.Enterprise.Services
{
    // These are referenced in the controller. They should probably be in a Models namespace,
    // but the controller uses them from the service namespace or via usings I need to verify.
    // Looking at EnterpriseController.cs usings:
    // using HelpDeskSystem.Enterprise.Services;
    
    public class HREmployee
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
    }

    public class HREmployeeSearchCriteria
    {
        public string? Name { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Status { get; set; }
    }

    public class HROnboardingRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public List<string> RequiredEquipment { get; set; } = new();
        public List<string> RequiredAccess { get; set; } = new();
    }

    public class HROffboardingRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public DateTime LastWorkingDay { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsVoluntary { get; set; }
    }

    public class HRLeaveRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class HRPerformanceReview
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string ReviewerId { get; set; } = string.Empty;
        public string ReviewPeriod { get; set; } = string.Empty;
        public int OverallRating { get; set; }
        public string Goals { get; set; } = string.Empty;
        public string Achievements { get; set; } = string.Empty;
        public string AreasForImprovement { get; set; } = string.Empty;
    }

    public class HRSalaryAdjustment
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string AdjustmentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // Duplicate - more complete definition exists later in file
    // public class SecurityIncident
    // {
    //     public string Title { get; set; } = string.Empty;
    //     public string Description { get; set; } = string.Empty;
    //     public string ReportedBy { get; set; } = string.Empty;
    //     public List<string> AffectedSystems { get; set; } = new();
    //     public string Severity { get; set; } = string.Empty;
    //     public string Status { get; set; } = string.Empty;
    // }

    public class SecurityIncidentFilter
    {
        public string? Status { get; set; }
        public string? Severity { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ThreatIntelligenceData
    {
        public string Source { get; set; } = string.Empty;
        public string ThreatType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public List<string> IoCs { get; set; } = new();
        public List<string> Indicators { get; set; } = new();
    }

    public class VulnerabilityScanRequest
    {
        public string Target { get; set; } = string.Empty;
        public string ScanType { get; set; } = string.Empty;
        public string Depth { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
    }

    public class SecurityAssessmentRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public string AssessorId { get; set; } = string.Empty;
    }

    public class SecurityPolicy
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Controls { get; set; } = new();
        public string Enforcement { get; set; } = string.Empty;
    }

    // Duplicate - more complete definition exists later in file
    // public class ITOMService
    // {
    //     public string Name { get; set; } = string.Empty;
    //     public string Description { get; set; } = string.Empty;
    //     public string Category { get; set; } = string.Empty;
    //     public string Owner { get; set; } = string.Empty;
    //     public string SLA { get; set; } = string.Empty;
    //     public List<string> Dependencies { get; set; } = new();
    // }

    public class ITOMServiceFilter
    {
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? Owner { get; set; }
    }

    // Duplicate - more complete definition exists later in file
    // public class RiskAssessment
    // {
    //     public string Title { get; set; } = string.Empty;
    //     public string Description { get; set; } = string.Empty;
    //     public string RiskCategory { get; set; } = string.Empty;
    //     public string InherentRisk { get; set; } = string.Empty;
    //     public List<string> RiskFactors { get; set; } = new();
    //     public List<string> Controls { get; set; } = new();
    // }

    public class RiskFilter
    {
        public string? Category { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    // Duplicate - more complete definition exists later in file
    // public class ComplianceControl
    // {
    //     public string Name { get; set; } = string.Empty;
    //     public string Description { get; set; } = string.Empty;
    //     public string Framework { get; set; } = string.Empty;
    //     public string ControlType { get; set; } = string.Empty;
    //     public string Frequency { get; set; } = string.Empty;
    //     public string Owner { get; set; } = string.Empty;
    // }

    public class ComplianceFilter
    {
        public string? Framework { get; set; }
        public string? ControlType { get; set; }
        public string? Status { get; set; }
    }

    public class AuditRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string AuditorId { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string Duration { get; set; } = string.Empty;
    }

    // Duplicate - more complete definition exists later in file
    // public class PolicyManagement
    // {
    //     public string Name { get; set; } = string.Empty;
    //     public string Description { get; set; } = string.Empty;
    //     public string Category { get; set; } = string.Empty;
    //     public DateTime EffectiveDate { get; set; }
    //     public string Owner { get; set; } = string.Empty;
    //     public string Content { get; set; } = string.Empty;
    // }

    // Duplicate - more complete definition exists later in file
    // public class WorkplaceService
    // {
    //     public string Name { get; set; } = string.Empty;
    //     public string Description { get; set; } = string.Empty;
    //     public string Category { get; set; } = string.Empty;
    //     public string Location { get; set; } = string.Empty;
    //     public int Capacity { get; set; }
    //     public decimal Cost { get; set; }
    // }

    public class WorkplaceFilter
    {
        public string? Category { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
    }

    public class FacilityManagement
    {
        public string FacilityId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public List<string> Amenities { get; set; } = new();
    }

    public class SpaceManagement
    {
        public string SpaceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<string> Equipment { get; set; } = new();
    }

    public class EquipmentManagement
    {
        public string EquipmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
    }

    public class ServiceRequest
    {
        public string ServiceId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public Dictionary<string, string> RequestData { get; set; } = new();
    }

    public class FieldServiceRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public Location GeocodedLocation { get; set; } = new();
        public List<Technician> AvailableTechnicians { get; set; } = new();
        public ServiceRoute OptimizedRoute { get; set; } = new();
        public Technician AssignedTechnician { get; set; } = new();
        public DateTime? AssignedAt { get; set; }
    }

    public class FieldFilter
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? TechnicianId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class WorkOrder
    {
        public string RequestId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tasks { get; set; } = new();
        public string EstimatedDuration { get; set; } = string.Empty;
        public List<string> Parts { get; set; } = new();
    }

    public class InventoryManagement
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CustomApplication
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> Entities { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Workflows { get; set; } = new();
        public List<string> Forms { get; set; } = new();
        public List<string> Reports { get; set; } = new();
    }

    // Security Operations Models
    public class SecurityIncident
    {
        public string IncidentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ReportedBy { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public List<string> Evidence { get; set; } = new();
        public List<string> AffectedSystems { get; set; } = new();
        public string Resolution { get; set; } = string.Empty;
        public DateTime? ResolvedAt { get; set; }
    }

    public class SecurityFilter
    {
        public string? Status { get; set; }
        public string? Severity { get; set; }
        public string? Category { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class SecurityEvidence
    {
        public string EvidenceId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CollectedAt { get; set; }
        public string CollectedBy { get; set; } = string.Empty;
    }

    public class ThreatIntelligence
    {
        public string ThreatId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public double RiskScore { get; set; }
        public List<string> ThreatPatterns { get; set; } = new();
        public List<string> MitigationStrategies { get; set; } = new();
        public DateTime ProcessedAt { get; set; }
    }

    public class ThreatPattern
    {
        public string PatternId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PatternType { get; set; } = string.Empty;
    }

    public class MitigationStrategy
    {
        public string StrategyId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
    }

    public class VulnerabilityAssessment
    {
        public string AssessmentId { get; set; } = string.Empty;
        public string TargetSystem { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<Vulnerability> Vulnerabilities { get; set; } = new();
        public RemediationPlan RemediationPlan { get; set; } = new();
    }

    public class Vulnerability
    {
        public string VulnerabilityId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public double RiskScore { get; set; }
        public string AffectedComponent { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    public class RemediationPlan
    {
        public string PlanId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public DateTime TargetDate { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
    }

    public class ResponsePlan
    {
        public string PlanId { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public List<string> ResponseSteps { get; set; } = new();
        public List<string> EscalationContacts { get; set; } = new();
        public string CommunicationPlan { get; set; } = string.Empty;
    }

    public class SecurityIncidentResponse
    {
        public string ResponseId { get; set; } = string.Empty;
        public string IncidentId { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public DateTime ResponseTime { get; set; }
        public string RespondedBy { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
    }

    // IT Operations Models
    public class ITOMService
    {
        public string ServiceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string ServiceLevel { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SLA { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
    }

    public class ITOMFilter
    {
        public string? Status { get; set; }
        public string? ServiceLevel { get; set; }
        public string? Owner { get; set; }
    }

    public class InfrastructureMonitoring
    {
        public string ComponentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public ComponentMetrics Metrics { get; set; } = new();
        public List<ComponentAlert> Alerts { get; set; } = new();
        public List<ComponentDependency> Dependencies { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class ComponentMetrics
    {
        public double CPUUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkUsage { get; set; }
        public int ActiveConnections { get; set; }
    }

    public class ComponentAlert
    {
        public string AlertId { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }
        public bool Resolved { get; set; }
    }

    public class ComponentDependency
    {
        public string SourceComponent { get; set; } = string.Empty;
        public string TargetComponent { get; set; } = string.Empty;
        public string DependencyType { get; set; } = string.Empty;
    }

    public class PerformanceMonitoring
    {
        public string MonitoringId { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double Threshold { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AutomatedRemediation
    {
        public string RemediationId { get; set; } = string.Empty;
        public string TriggerCondition { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public bool Executed { get; set; }
        public DateTime ExecutedAt { get; set; }
    }

    public class CapacityPlanning
    {
        public string PlanId { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public double CurrentCapacity { get; set; }
        public double ProjectedDemand { get; set; }
        public DateTime ForecastDate { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    // GRC Models
    public class RiskAssessment
    {
        public string AssessmentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<RiskFactor> RiskFactors { get; set; } = new();
        public string RiskCategory { get; set; } = string.Empty;
        public string InherentRisk { get; set; } = string.Empty;
        public List<string> Controls { get; set; } = new();
        public double OverallRiskScore { get; set; }
        public RiskTreatment RiskTreatment { get; set; } = new();
        public MitigationPlan MitigationPlan { get; set; } = new();
    }

    public class RiskFactor
    {
        public string FactorId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Likelihood { get; set; }
        public double Impact { get; set; }
        public double RiskScore { get; set; }
    }

    public class RiskTreatment
    {
        public string TreatmentId { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public DateTime TargetDate { get; set; }
    }

    public class ComplianceControl
    {
        public string ControlId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Frameworks { get; set; } = new();
        public string Framework { get; set; } = string.Empty;
        public string ControlType { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public List<ControlTest> Tests { get; set; } = new();
    }

    public class ControlTest
    {
        public string TestId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TestType { get; set; } = string.Empty;
        public DateTime TestDate { get; set; }
        public string Result { get; set; } = string.Empty;
    }

    public class AuditTrail
    {
        public string AuditId { get; set; } = string.Empty;
        public string AuditType { get; set; } = string.Empty;
        public AuditRequest Request { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string AuditorId { get; set; } = string.Empty;
        public List<AuditResult> Results { get; set; } = new();
        public List<AuditFinding> Findings { get; set; } = new();
        public List<AuditRecommendation> Recommendations { get; set; } = new();
    }

    public class AuditResult
    {
        public string ResultId { get; set; } = string.Empty;
        public string ControlId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public class AuditFinding
    {
        public string FindingId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public bool Resolved { get; set; }
    }

    public class PolicyManagement
    {
        public string PolicyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public string Owner { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public enum ReportType { SOX, GDPR, HIPAA, ISO27001, PCI_DSS }

    public class ComplianceReport
    {
        public string ReportId { get; set; } = string.Empty;
        public ReportType ReportType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public double ComplianceScore { get; set; }
        public List<AuditFinding> Findings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    // Workplace Service Models
    public class WorkplaceService
    {
        public string ServiceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal Cost { get; set; }
    }

    public class WorkplaceAnalytics
    {
        public string LocationId { get; set; } = string.Empty;
        public double UtilizationRate { get; set; }
        public int ActiveUsers { get; set; }
        public int AvailableSpaces { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    // Field Service Models
    public class FieldTechnician
    {
        public string TechnicianId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public string CurrentLocation { get; set; } = string.Empty;
        public List<ServiceRequest> Schedule { get; set; } = new();
    }

    public class WorkOrderManagement
    {
        public string WorkOrderId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string AssignedTechnician { get; set; } = string.Empty;
    }

    public class MobileFieldApp
    {
        public string AppId { get; set; } = string.Empty;
        public string TechnicianId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime LastSync { get; set; }
        public List<string> AvailableFeatures { get; set; } = new();
    }

    // Low-Code Platform Models
    public class WorkflowBuilder
    {
        public string WorkflowId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<WorkflowStep> Steps { get; set; } = new();
        public WorkflowUI UI { get; set; } = new();
    }

    public class WorkflowStep
    {
        public string StepId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    public class FormBuilder
    {
        public string FormId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<FormField> Fields { get; set; } = new();
    }

    public class FormField
    {
        public string FieldId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public Dictionary<string, object> Options { get; set; } = new();
    }

    public class ReportBuilder
    {
        public string ReportId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ReportField> Fields { get; set; } = new();
        public string DataSource { get; set; } = string.Empty;
    }

    public class ReportField
    {
        public string FieldId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Aggregation { get; set; } = string.Empty;
    }

    // Integration Hub Models
    public class IntegrationConnector
    {
        public string ConnectorId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<DataMapping> DataMappings { get; set; } = new();
        public ConnectionTest ConnectionTest { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public object? Configuration { get; set; }
        public object? Authentication { get; set; }
        public object? DataMapping { get; set; }
    }

    public class ConnectionTest
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime TestedAt { get; set; }
        public int ResponseTimeMs { get; set; }
    }

    public enum OverallHealth
    {
        Healthy,
        Warning,
        Critical,
        Unhealthy
    }

    public class DataMapping
    {
        public string MappingId { get; set; } = string.Empty;
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public string Transformation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SourceConnector { get; set; } = string.Empty;
        public string TargetConnector { get; set; } = string.Empty;
        public Dictionary<string, string> FieldMappings { get; set; } = new();
    }

    public class APIManagement
    {
        public string ApiId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string AuthenticationType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Authentication { get; set; } = string.Empty;
        public string RateLimit { get; set; } = string.Empty;
    }

    public class WebhookManagement
    {
        public string WebhookId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public bool Active { get; set; }
        public List<string> Events { get; set; } = new();
        public string Secret { get; set; } = string.Empty;
    }

    public class IntegrationMonitoring
    {
        public string MonitoringId { get; set; } = string.Empty;
        public string OverallStatus { get; set; } = string.Empty;
        public List<ConnectorHealth> Connectors { get; set; } = new();
        public List<DataFlow> DataFlows { get; set; } = new();
        public List<ErrorRate> ErrorRates { get; set; } = new();
        public List<Latency> Latencies { get; set; } = new();
        public List<Throughput> Throughput { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class ConnectorHealth
    {
        public string ConnectorId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class DataFlow
    {
        public string FlowId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public double Throughput { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class IntegrationBuilder
    {
        public string IntegrationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> SourceSystems { get; set; } = new();
        public List<string> TargetSystems { get; set; } = new();
        public List<DataMapping> Mappings { get; set; } = new();
    }

    // Enterprise Analytics Models
    public class EnterpriseKPI
    {
        public string KPIId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Value { get; set; }
        public double Target { get; set; }
        public double Variance { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class OperationalMetrics
    {
        public double IncidentResolutionRate { get; set; }
        public double SLAComplianceRate { get; set; }
        public double SystemAvailability { get; set; }
        public double UserSatisfaction { get; set; }
    }

    public class FinancialMetrics
    {
        public double TotalRevenue { get; set; }
        public double OperatingCosts { get; set; }
        public double ProfitMargin { get; set; }
        public double CostPerTicket { get; set; }
    }

    public class CustomerMetrics
    {
        public double CSATScore { get; set; }
        public double NPS { get; set; }
        public double CustomerRetention { get; set; }
        public double FirstContactResolution { get; set; }
    }

    public class EmployeeMetrics
    {
        public double Productivity { get; set; }
        public double Satisfaction { get; set; }
        public double Retention { get; set; }
        public double TrainingCompletion { get; set; }
    }

    public class RiskMetrics
    {
        public double OverallRiskLevel { get; set; }
        public int ActiveRisks { get; set; }
        public int MitigatedRisks { get; set; }
        public double ComplianceScore { get; set; }
    }

    public class ComplianceMetrics
    {
        public double PolicyCompliance { get; set; }
        public double AuditPassRate { get; set; }
        public int OpenFindings { get; set; }
        public int ResolvedFindings { get; set; }
    }

    public class EnterpriseAlert
    {
        public string AlertId { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }
        public string Source { get; set; } = string.Empty;
        public bool Acknowledged { get; set; }
    }

    public class CustomEntity
    {
        public string EntityId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class CustomWorkflow
    {
        public string WorkflowId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
    }

    public class CustomForm
    {
        public string FormId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public List<string> Fields { get; set; } = new();
    }

    public class CustomReport
    {
        public string ReportId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public List<string> Metrics { get; set; } = new();
    }

    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
    }

    public class ServiceRoute
    {
        public string RouteId { get; set; } = string.Empty;
        public List<Location> Waypoints { get; set; } = new();
        public double EstimatedDuration { get; set; }
        public double Distance { get; set; }
    }

    public class Technician
    {
        public string TechnicianId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
    }

    public class WorkflowUI
    {
        public string UIId { get; set; } = string.Empty;
        public string Layout { get; set; } = string.Empty;
        public List<string> Components { get; set; } = new();
    }

    public class BusinessIntelligence
    {
        public string ReportId { get; set; } = string.Empty;
        public BIRequest Request { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public List<ReportData> Data { get; set; } = new();
        public List<Visualization> Visualizations { get; set; } = new();
        public List<string> Insights { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class ReportData
    {
        public string DataId { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class Visualization
    {
        public string VisualizationId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    public class Insight
    {
        public string InsightId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class BIRecommendation
    {
        public string RecommendationId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    public class EnterpriseDashboard
    {
        public DateTime GeneratedAt { get; set; }
        public List<EnterpriseKPI> KPIs { get; set; } = new();
        public OperationalMetrics OperationalMetrics { get; set; } = new();
        public FinancialMetrics FinancialMetrics { get; set; } = new();
        public CustomerMetrics CustomerMetrics { get; set; } = new();
        public EmployeeMetrics EmployeeMetrics { get; set; } = new();
        public RiskMetrics RiskMetrics { get; set; } = new();
        public ComplianceMetrics ComplianceMetrics { get; set; } = new();
        public List<EnterpriseAlert> Alerts { get; set; } = new();
        public object? HRMetrics { get; set; }
        public object? SecurityMetrics { get; set; }
        public object? ITOMMetrics { get; set; }
        public object? GRCMetrics { get; set; }
        public object? WorkplaceMetrics { get; set; }
        public object? FieldServiceMetrics { get; set; }
        public object? LowCodeMetrics { get; set; }
        public object? IntegrationMetrics { get; set; }
    }

    // Additional models from duplicate definitions
    public class AppFilter
    {
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class WorkflowDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public Dictionary<string, string> Variables { get; set; } = new();
    }

    public class FormDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Fields { get; set; } = new();
        public string Validation { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
    }

    public class ReportDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public List<string> Fields { get; set; } = new();
        public List<string> Filters { get; set; } = new();
        public string Grouping { get; set; } = string.Empty;
    }

    // Duplicate - more complete definition exists earlier in file
    // public class IntegrationConnector
    // {
    //     public string Name { get; set; } = string.Empty;
    //     public string Type { get; set; } = string.Empty;
    //     public string Description { get; set; } = string.Empty;
    //     public Dictionary<string, string> Configuration { get; set; } = new();
    //     public string Authentication { get; set; } = string.Empty;
    //     public string DataMapping { get; set; } = string.Empty;
    // }

    public class ConnectorFilter
    {
        public string? Type { get; set; }
        public string? Status { get; set; }
    }

    // Duplicate - defined earlier in this file
    // public class DataMapping
    // {
    //     public string Name { get; set; } = string.Empty;
    //     public string SourceConnector { get; set; } = string.Empty;
    //     public string TargetConnector { get; set; } = string.Empty;
    //     public Dictionary<string, string> FieldMappings { get; set; } = new();
    // }

    // Duplicate - more complete definition exists earlier in file
    // Commented out to prevent duplicate definition errors
    //
    // public class APIManagement
    // {
    //     public string ApiId { get; set; } = string.Empty;
    //     public string Name { get; set; } = string.Empty;
    //     public string Endpoint { get; set; } = string.Empty;
    //     public string Method { get; set; } = string.Empty;
    //     public string Authentication { get; set; } = string.Empty;
    //     public string RateLimit { get; set; } = string.Empty;
    // }
    //
    // public class WebhookManagement
    // {
    //     public string WebhookId { get; set; } = string.Empty;
    //     public string Name { get; set; } = string.Empty;
    //     public string Url { get; set; } = string.Empty;
    //     public List<string> Events { get; set; } = new();
    //     public string Secret { get; set; } = string.Empty;
    //     public bool Active { get; set; }
    // }

    public class HRMetrics { }
    public class SecurityMetrics { }
    public class ITOMMetrics { }
    public class GRCMetrics { }
    public class WorkplaceMetrics { }
    public class FieldServiceMetrics { }
    public class LowCodeMetrics { }
    public class IntegrationMetrics { }

    public class PredictiveModel
    {
        public string ModelId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class PredictiveAnalytics
    {
        public string AnalyticsId { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public List<PredictionResult> Results { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public double Confidence { get; set; }
    }

    public class PredictionResult
    {
        public string Metric { get; set; } = string.Empty;
        public double PredictedValue { get; set; }
        public DateTime PredictionFor { get; set; }
    }

    public class KPI
    {
        public string KPIId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public double Value { get; set; }
        public double Target { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime MeasuredAt { get; set; }
        public List<KPITrend> Trends { get; set; } = new();
    }

    public class KPITrend
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }

    public class ExecutiveSummary
    {
        public string SummaryId { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<KPI> KeyMetrics { get; set; } = new();
        public List<Highlight> Highlights { get; set; } = new();
        public List<Alert> Alerts { get; set; } = new();
        public string Narrative { get; set; } = string.Empty;
    }

    public class Highlight
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class Alert
    {
        public string AlertId { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class Throughput
    {
        public string Id { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public double RequestsPerSecond { get; set; }
        public long TotalRequests { get; set; }
        public DateTime MeasuredAt { get; set; }
    }

    public class SLABreach
    {
        public string BreachId { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public DateTime BreachTime { get; set; }
        public TimeSpan BreachDuration { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
    }

    // Duplicate class commented out
    // public class DataFlow
    // {
    //     public string FlowId { get; set; } = string.Empty;
    //     public string Source { get; set; } = string.Empty;
    //     public string Target { get; set; } = string.Empty;
    //     public double Throughput { get; set; }
    //     public DateTime Timestamp { get; set; }
    // }

    public class ErrorRate
    {
        public string Id { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public double Rate { get; set; }
        public int ErrorCount { get; set; }
        public int TotalCount { get; set; }
        public DateTime MeasuredAt { get; set; }
    }

    public class Latency
    {
        public string Id { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public double AverageMs { get; set; }
        public double P95Ms { get; set; }
        public double P99Ms { get; set; }
        public DateTime MeasuredAt { get; set; }
    }

    public class BIRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public enum SecuritySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum ComponentHealth
    {
        Healthy,
        Degraded,
        Unhealthy,
        Critical
    }
    public class MitigationPlan { }
    public class AuditRecommendation { }
}
