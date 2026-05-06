using System;
using System.Collections.Generic;

namespace HelpDeskSystem.API.DTOs.Enterprise
{
    // HR DTOs
    public class CreateEmployeeDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
    }

    public class UpdateEmployeeDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
    }

    public class EmployeeSearchDto
    {
        public string? Name { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Status { get; set; }
    }

    public class OnboardingRequestDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public List<string> RequiredEquipment { get; set; } = new();
        public List<string> RequiredAccess { get; set; } = new();
    }

    public class OffboardingRequestDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public DateTime LastWorkingDay { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsVoluntary { get; set; }
    }

    public class LeaveRequestDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class PerformanceReviewDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string ReviewerId { get; set; } = string.Empty;
        public string ReviewPeriod { get; set; } = string.Empty;
        public int OverallRating { get; set; }
        public string Goals { get; set; } = string.Empty;
        public string Achievements { get; set; } = string.Empty;
        public string AreasForImprovement { get; set; } = string.Empty;
    }

    public class SalaryAdjustmentDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string AdjustmentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // Security DTOs
    public class SecurityIncidentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportedBy { get; set; } = string.Empty;
        public List<string> AffectedSystems { get; set; } = new();
        public string Severity { get; set; } = string.Empty;
    }

    public class UpdateSecurityIncidentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public class SecurityIncidentFilterDto
    {
        public string? Status { get; set; }
        public string? Severity { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ThreatIntelligenceDto
    {
        public string Source { get; set; } = string.Empty;
        public string ThreatType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public List<string> IoCs { get; set; } = new();
        public List<string> Indicators { get; set; } = new();
    }

    public class VulnerabilityScanDto
    {
        public string Target { get; set; } = string.Empty;
        public string ScanType { get; set; } = string.Empty;
        public string Depth { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
    }

    public class SecurityAssessmentDto
    {
        public string Type { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public string AssessorId { get; set; } = string.Empty;
    }

    public class SecurityPolicyDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Controls { get; set; } = new();
        public string Enforcement { get; set; } = string.Empty;
    }

    // ITOM DTOs
    public class ITOMServiceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string SLA { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
    }

    public class UpdateITOMServiceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string SLA { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
    }

    public class ITOMServiceFilterDto
    {
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? Owner { get; set; }
    }

    // GRC DTOs
    public class RiskAssessmentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RiskCategory { get; set; } = string.Empty;
        public string InherentRisk { get; set; } = string.Empty;
        public List<string> RiskFactors { get; set; } = new();
        public List<string> Controls { get; set; } = new();
    }

    public class UpdateRiskAssessmentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RiskCategory { get; set; } = string.Empty;
        public string InherentRisk { get; set; } = string.Empty;
        public List<string> RiskFactors { get; set; } = new();
        public List<string> Controls { get; set; } = new();
    }

    public class RiskFilterDto
    {
        public string? Category { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ComplianceControlDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public string ControlType { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
    }

    public class ComplianceFilterDto
    {
        public string? Framework { get; set; }
        public string? ControlType { get; set; }
        public string? Status { get; set; }
    }

    public class AuditRequestDto
    {
        public string Type { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string AuditorId { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string Duration { get; set; } = string.Empty;
    }

    public class PolicyManagementDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public string Owner { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    // Workplace DTOs
    public class WorkplaceServiceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal Cost { get; set; }
    }

    public class WorkplaceFilterDto
    {
        public string? Category { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
    }

    public class FacilityManagementDto
    {
        public string FacilityId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public List<string> Amenities { get; set; } = new();
    }

    public class SpaceManagementDto
    {
        public string SpaceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<string> Equipment { get; set; } = new();
    }

    public class EquipmentManagementDto
    {
        public string EquipmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
    }

    public class ServiceRequestDto
    {
        public string ServiceId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public Dictionary<string, string> RequestData { get; set; } = new();
    }

    // Field Service DTOs
    public class FieldServiceRequestDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
    }

    public class FieldFilterDto
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? TechnicianId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class AssignTechnicianDto
    {
        public string TechnicianId { get; set; } = string.Empty;
    }

    public class WorkOrderDto
    {
        public string RequestId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tasks { get; set; } = new();
        public string EstimatedDuration { get; set; } = string.Empty;
        public List<string> Parts { get; set; } = new();
    }

    public class InventoryManagementDto
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class FieldAnalyticsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class RouteOptimizationDto
    {
        public List<string> Requests { get; set; } = new();
    }

    // Low-Code DTOs
    public class CustomApplicationDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> Entities { get; set; } = new();
        public List<string> Workflows { get; set; } = new();
        public List<string> Forms { get; set; } = new();
        public List<string> Reports { get; set; } = new();
    }

    public class AppFilterDto
    {
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class WorkflowDefinitionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public Dictionary<string, string> Variables { get; set; } = new();
    }

    public class FormDefinitionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Fields { get; set; } = new();
        public string Validation { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
    }

    public class ReportDefinitionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public List<string> Fields { get; set; } = new();
        public List<string> Filters { get; set; } = new();
        public string Grouping { get; set; } = string.Empty;
    }

    // Integration DTOs
    public class IntegrationConnectorDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> Configuration { get; set; } = new();
        public string Authentication { get; set; } = string.Empty;
        public string DataMapping { get; set; } = string.Empty;
    }

    public class ConnectorFilterDto
    {
        public string? Type { get; set; }
        public string? Status { get; set; }
    }

    public class DataMappingDto
    {
        public string Name { get; set; } = string.Empty;
        public string SourceConnector { get; set; } = string.Empty;
        public string TargetConnector { get; set; } = string.Empty;
        public Dictionary<string, string> FieldMappings { get; set; } = new();
    }

    public class APIManagementDto
    {
        public string ApiId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Authentication { get; set; } = string.Empty;
        public string RateLimit { get; set; } = string.Empty;
    }

    public class WebhookManagementDto
    {
        public string WebhookId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public List<string> Events { get; set; } = new();
        public string Secret { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
