using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HelpDeskSystem.Enterprise.Services
{
    public interface IRealEnterpriseModulesService
    {
        // HR
        Task<object> CreateEmployeeRecord(HREmployee employee);
        Task<object> UpdateEmployeeRecord(string employeeId, HREmployee employee);
        Task<object> GetEmployeeRecord(string employeeId);
        Task<IEnumerable<object>> SearchEmployees(HREmployeeSearchCriteria criteria);
        Task<object> InitiateOnboarding(HROnboardingRequest request);
        Task<object> InitiateOffboarding(HROffboardingRequest request);
        Task<object> SubmitLeaveRequest(HRLeaveRequest request);
        Task<object> CreatePerformanceReview(HRPerformanceReview review);
        Task<object> ProcessSalaryAdjustment(HRSalaryAdjustment adjustment);

        // Security
        Task<object> CreateSecurityIncident(SecurityIncident incident);
        Task<object> UpdateSecurityIncident(string incidentId, SecurityIncident incident);
        Task<IEnumerable<object>> GetSecurityIncidents(SecurityIncidentFilter? filter);
        Task<object> ProcessThreatIntelligence(ThreatIntelligenceData data);
        Task<object> ExecuteVulnerabilityScan(VulnerabilityScanRequest request);
        Task<object> ConductSecurityAssessment(SecurityAssessmentRequest request);
        Task<object> CreateSecurityPolicy(SecurityPolicy policy);
        Task<IEnumerable<object>> GetSecurityPolicies();

        // ITOM
        Task<object> CreateITOMService(ITOMService service);
        Task<object> UpdateITOMService(string serviceId, ITOMService service);
        Task<IEnumerable<object>> GetITOMServices(ITOMServiceFilter? filter);
        Task<object> GetInfrastructureHealth();
        Task<object> GetPerformanceMetrics(string serviceId);
        Task<object> TriggerAutomatedRemediation(string alertId);
        Task<object> GetITOMDashboard();

        // GRC
        Task<object> CreateRiskAssessment(RiskAssessment assessment);
        Task<object> UpdateRiskAssessment(string assessmentId, RiskAssessment assessment);
        Task<IEnumerable<object>> GetRiskAssessments(RiskFilter? filter);
        Task<object> CreateComplianceControl(ComplianceControl control);
        Task<IEnumerable<object>> GetComplianceControls(ComplianceFilter? filter);
        Task<object> ConductAudit(AuditRequest request);
        Task<object> CreatePolicy(PolicyManagement policy);

        // Workplace
        Task<object> CreateWorkplaceService(WorkplaceService service);
        Task<IEnumerable<object>> GetWorkplaceServices(WorkplaceFilter? filter);
        Task<object> ManageFacility(FacilityManagement facility);
        Task<object> ManageWorkspace(SpaceManagement space);
        Task<object> ManageEquipment(EquipmentManagement equipment);
        Task<object> GetWorkplaceAnalytics(string locationId);
        Task<object> CreateServiceRequest(ServiceRequest request);

        // Field Service
        Task<object> CreateFieldServiceRequest(FieldServiceRequest request);
        Task<IEnumerable<object>> GetFieldServiceRequests(FieldFilter? filter);
        Task<object> AssignTechnician(string requestId, string technicianId);
        Task<object> CreateWorkOrder(WorkOrder workOrder);
        Task<object> ManageFieldInventory(InventoryManagement inventory);
        Task<object> GetFieldServiceAnalytics(DateTime startDate, DateTime endDate);
        Task<object> OptimizeRoutes(List<string> requests);

        // Low-Code
        Task<object> CreateCustomApplication(CustomApplication app);
        Task<IEnumerable<object>> GetCustomApplications(AppFilter? filter);
        Task<object> CreateWorkflow(WorkflowDefinition workflow);
        Task<object> CreateForm(FormDefinition form);
        Task<object> CreateReport(ReportDefinition report);

        // Integration
        Task<object> CreateIntegrationConnector(IntegrationConnector connector);
        Task<IEnumerable<object>> GetIntegrationConnectors(ConnectorFilter? filter);
        Task<object> CreateDataMapping(DataMapping mapping);
        Task<object> ManageAPI(APIManagement api);
        Task<object> ManageWebhook(WebhookManagement webhook);
        Task<object> GetIntegrationHealth();
        Task<object> TestIntegration(string connectorId);
    }
}
