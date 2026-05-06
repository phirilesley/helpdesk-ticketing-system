using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using HelpDeskSystem.ITSM.Services;
using HelpDeskSystem.API.DTOs.ITSM;

namespace HelpDeskSystem.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/itsm")]
    public class ITSMController : ControllerBase
    {
        private readonly IITSMService _itsmService;
        private readonly IITILComplianceService _itilComplianceService;

        public ITSMController(IITSMService itsmService, IITILComplianceService itilComplianceService)
        {
            _itsmService = itsmService;
            _itilComplianceService = itilComplianceService;
        }

        // Incident Management
        [HttpPost("incidents")]
        public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentDto request)
        {
            var incident = new Incident
            {
                Title = request.Title,
                Description = request.Description,
                PriorityId = request.PriorityId,
                CategoryId = request.CategoryId,
                Impact = request.Impact,
                Urgency = request.Urgency,
                ReportedByUserId = request.ReportedByUserId
            };
            var result = await _itsmService.CreateIncident(incident);
            return Ok(result);
        }

        [HttpPut("incidents/{incidentId}")]
        public async Task<IActionResult> UpdateIncident(int incidentId, [FromBody] UpdateIncidentDto request)
        {
            var incident = new Incident
            {
                Title = request.Title,
                Description = request.Description,
                PriorityId = request.PriorityId,
                CategoryId = request.CategoryId,
                Impact = request.Impact,
                Urgency = request.Urgency
            };
            var result = await _itsmService.UpdateIncident(incidentId, incident);
            return Ok(result);
        }

        [HttpGet("incidents")]
        public async Task<IActionResult> GetIncidents([FromQuery] IncidentFilterDto filter)
        {
            var itsmFilter = filter != null ? new IncidentFilter
            {
                Status = filter.Status,
                Priority = filter.Priority,
                AssignedTo = filter.AssignedTo,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate
            } : null;
            var incidents = await _itsmService.GetIncidents(itsmFilter);
            return Ok(incidents);
        }

        [HttpPost("incidents/{incidentId}/assign")]
        public async Task<IActionResult> AssignIncident(int incidentId, [FromBody] AssignIncidentDto request)
        {
            var incident = await _itsmService.AssignIncident(incidentId, request.AssigneeId, request.AssignmentGroup);
            return Ok(incident);
        }

        [HttpPost("incidents/{incidentId}/escalate")]
        public async Task<IActionResult> EscalateIncident(int incidentId, [FromBody] EscalateIncidentDto request)
        {
            var incident = await _itsmService.EscalateIncident(incidentId, request.Reason, request.EscalationLevel);
            return Ok(incident);
        }

        [HttpPost("incidents/{incidentId}/resolve")]
        public async Task<IActionResult> ResolveIncident(int incidentId, [FromBody] ResolveIncidentDto request)
        {
            var incident = await _itsmService.ResolveIncident(incidentId, request.Resolution, request.ResolutionCode);
            return Ok(incident);
        }

        [HttpPost("incidents/{incidentId}/close")]
        public async Task<IActionResult> CloseIncident(int incidentId, [FromBody] CloseIncidentDto request)
        {
            var incident = await _itsmService.CloseIncident(incidentId, request.ClosureCode, request.SatisfactionRating);
            return Ok(incident);
        }

        // Problem Management
        [HttpPost("problems")]
        public async Task<IActionResult> CreateProblem([FromBody] CreateProblemDto request)
        {
            var problem = new Problem
            {
                Title = request.Title,
                Description = request.Description,
                PriorityId = request.PriorityId,
                CategoryId = request.CategoryId
            };
            var result = await _itsmService.CreateProblem(problem);
            return Ok(result);
        }

        [HttpPut("problems/{problemId}")]
        public async Task<IActionResult> UpdateProblem(int problemId, [FromBody] UpdateProblemDto request)
        {
            var problem = new Problem
            {
                Title = request.Title,
                Description = request.Description,
                PriorityId = request.PriorityId,
                CategoryId = request.CategoryId
            };
            var result = await _itsmService.UpdateProblem(problemId, problem);
            return Ok(result);
        }

        [HttpGet("problems")]
        public async Task<IActionResult> GetProblems([FromQuery] ProblemFilterDto filter)
        {
            var itsmFilter = filter != null ? new ProblemFilter
            {
                Status = filter.Status,
                Priority = filter.Priority,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate
            } : null;
            var problems = await _itsmService.GetProblems(itsmFilter);
            return Ok(problems);
        }

        [HttpPost("problems/{problemId}/link-incident")]
        public async Task<IActionResult> LinkIncidentToProblem(int problemId, [FromBody] LinkIncidentDto request)
        {
            var problem = await _itsmService.LinkIncidentToProblem(request.IncidentId, problemId);
            return Ok(problem);
        }

        [HttpPost("problems/{problemId}/rca")]
        public async Task<IActionResult> PerformRootCauseAnalysis(int problemId, [FromBody] PerformRCADto request)
        {
            var rca = new RootCauseAnalysis
            {
                AnalysisMethod = request.AnalysisMethod,
                RootCause = request.RootCause,
                ContributingFactors = request.ContributingFactors,
                Recommendations = request.Recommendations
            };
            var problem = await _itsmService.PerformRootCauseAnalysis(problemId, rca);
            return Ok(problem);
        }

        [HttpPost("problems/{problemId}/permanent-fix")]
        public async Task<IActionResult> ImplementPermanentFix(int problemId, [FromBody] ImplementPermanentFixDto request)
        {
            var problem = await _itsmService.ImplementPermanentFix(problemId, request.FixDescription, request.ImplementationDate);
            return Ok(problem);
        }

        // Change Management
        [HttpPost("changes")]
        public async Task<IActionResult> CreateChangeRequest([FromBody] CreateChangeDto request)
        {
            var change = new ChangeRequest
            {
                Title = request.Title,
                Description = request.Description,
                PriorityId = request.PriorityId,
                CategoryId = request.CategoryId,
                ChangeType = request.ChangeType,
                RiskAssessment = request.RiskAssessment,
                ImpactAssessment = request.ImpactAssessment
            };
            var result = await _itsmService.CreateChangeRequest(change);
            return Ok(result);
        }

        [HttpPut("changes/{changeId}")]
        public async Task<IActionResult> UpdateChangeRequest(int changeId, [FromBody] UpdateChangeDto request)
        {
            var change = new ChangeRequest
            {
                Title = request.Title,
                Description = request.Description,
                PriorityId = request.PriorityId,
                CategoryId = request.CategoryId,
                ChangeType = request.ChangeType,
                RiskAssessment = request.RiskAssessment,
                ImpactAssessment = request.ImpactAssessment
            };
            var result = await _itsmService.UpdateChangeRequest(changeId, change);
            return Ok(result);
        }

        [HttpGet("changes")]
        public async Task<IActionResult> GetChangeRequests([FromQuery] ChangeFilterDto filter)
        {
            var itsmFilter = filter != null ? new ChangeFilter
            {
                Status = filter.Status,
                Type = filter.Type,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate
            } : null;
            var changes = await _itsmService.GetChangeRequests(itsmFilter);
            return Ok(changes);
        }

        [HttpPost("changes/{changeId}/submit-approval")]
        public async Task<IActionResult> SubmitForApproval(int changeId, [FromBody] SubmitApprovalDto request)
        {
            var change = await _itsmService.SubmitForApproval(changeId, request.Approvers);
            return Ok(change);
        }

        [HttpPost("changes/{changeId}/approve")]
        public async Task<IActionResult> ApproveChange(int changeId, [FromBody] ApproveChangeDto request)
        {
            var change = await _itsmService.ApproveChange(changeId, request.ApproverId, request.Comments);
            return Ok(change);
        }

        [HttpPost("changes/{changeId}/reject")]
        public async Task<IActionResult> RejectChange(int changeId, [FromBody] RejectChangeDto request)
        {
            var change = await _itsmService.RejectChange(changeId, request.ApproverId, request.Reason);
            return Ok(change);
        }

        [HttpPost("changes/{changeId}/schedule")]
        public async Task<IActionResult> ScheduleChange(int changeId, [FromBody] ScheduleChangeDto request)
        {
            var change = await _itsmService.ScheduleChange(changeId, request.ScheduledDate, request.EstimatedDuration);
            return Ok(change);
        }

        [HttpPost("changes/{changeId}/implement")]
        public async Task<IActionResult> ImplementChange(int changeId, [FromBody] ImplementChangeDto request)
        {
            var change = await _itsmService.ImplementChange(changeId, request.ImplementationDetails);
            return Ok(change);
        }

        [HttpPost("changes/{changeId}/review")]
        public async Task<IActionResult> ReviewChange(int changeId, [FromBody] ReviewChangeDto request)
        {
            var review = new ChangeReview
            {
                ReviewerId = request.ReviewerId,
                Successful = request.Successful,
                Comments = request.Comments,
                Issues = string.IsNullOrWhiteSpace(request.Issues)
                    ? new List<string>()
                    : new List<string> { request.Issues },
                ReviewedAt = DateTime.UtcNow
            };
            var change = await _itsmService.ReviewChange(changeId, review);
            return Ok(change);
        }

        // Asset Management (CMDB)
        [HttpPost("configuration-items")]
        public async Task<IActionResult> CreateConfigurationItem([FromBody] CreateCIDto request)
        {
            var ci = new ConfigurationItem
            {
                Name = request.Name,
                Description = request.Description,
                CIType = request.CIType,
                Owner = request.Owner,
                Location = request.Location,
                Attributes = request.Attributes
            };
            var result = await _itsmService.CreateConfigurationItem(ci);
            return Ok(result);
        }

        [HttpPut("configuration-items/{ciId}")]
        public async Task<IActionResult> UpdateConfigurationItem(int ciId, [FromBody] UpdateCIDto request)
        {
            var ci = new ConfigurationItem
            {
                Name = request.Name,
                Description = request.Description,
                CIType = request.CIType,
                Owner = request.Owner,
                Location = request.Location,
                Attributes = request.Attributes
            };
            var result = await _itsmService.UpdateConfigurationItem(ciId, ci);
            return Ok(result);
        }

        [HttpGet("configuration-items")]
        public async Task<IActionResult> GetConfigurationItems([FromQuery] CIFilterDto filter)
        {
            var itsmFilter = filter != null ? new CIFilter
            {
                CIType = filter.CIType,
                Status = filter.Status,
                Owner = filter.Owner,
                Location = filter.Location
            } : null;
            var cis = await _itsmService.GetConfigurationItems(itsmFilter);
            return Ok(cis);
        }

        [HttpGet("configuration-items/{ciId}/related")]
        public async Task<IActionResult> GetRelatedCIs(int ciId)
        {
            var cis = await _itsmService.GetRelatedCIs(ciId);
            return Ok(cis);
        }

        [HttpPost("configuration-items/link")]
        public async Task<IActionResult> LinkCIs([FromBody] LinkCIDto request)
        {
            var ci = await _itsmService.LinkCIs(request.SourceCiId, request.TargetCiId, request.RelationshipType);
            return Ok(ci);
        }

        [HttpPost("configuration-items/{ciId}/lifecycle")]
        public async Task<IActionResult> RecordAssetLifecycle(int ciId, [FromBody] AssetLifecycleDto request)
        {
            var parsed = Enum.TryParse<AssetLifecycleEvent>(request.LifecycleEvent, true, out var lifecycleEvent)
                ? lifecycleEvent
                : AssetLifecycleEvent.Deployed;
            var ci = await _itsmService.RecordAssetLifecycle(ciId, parsed);
            return Ok(ci);
        }

        // Service Catalog
        [HttpPost("services")]
        public async Task<IActionResult> CreateServiceCatalogItem([FromBody] CreateServiceDto request)
        {
            var service = new ServiceCatalogItem
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                Cost = request.Cost,
                Currency = request.Currency
            };
            var result = await _itsmService.CreateServiceCatalogItem(service);
            return Ok(result);
        }

        [HttpGet("services")]
        public async Task<IActionResult> GetServiceCatalog([FromQuery] ServiceFilterDto filter)
        {
            var itsmFilter = filter != null ? new ServiceFilter
            {
                Category = filter.Category,
                Status = filter.Status,
                MinCost = filter.MinCost,
                MaxCost = filter.MaxCost
            } : null;
            var services = await _itsmService.GetServiceCatalog(itsmFilter);
            return Ok(services);
        }

        // SLA Management
        [HttpPost("slas")]
        public async Task<IActionResult> CreateSLA([FromBody] CreateSLADto request)
        {
            var sla = new ServiceLevelAgreement
            {
                Name = request.Name,
                ServiceId = request.ServiceId,
                ResponseTime = request.ResponseTime,
                ResolutionTime = request.ResolutionTime,
                AvailabilityPercentage = request.AvailabilityPercentage
            };
            var result = await _itsmService.CreateSLA(sla);
            return Ok(result);
        }

        [HttpGet("slas")]
        public async Task<IActionResult> GetSLAs([FromQuery] SLAFilterDto filter)
        {
            var itsmFilter = filter != null ? new SLAFilter
            {
                ServiceId = filter.ServiceId?.ToString(),
                Status = filter.Status,
                ActiveFrom = filter.EffectiveDate
            } : null;
            var slas = await _itsmService.GetSLAs(itsmFilter);
            return Ok(slas);
        }

        [HttpPost("slas/{slaId}/breach")]
        public async Task<IActionResult> RecordSLABreach(int slaId, [FromBody] SLABreachDto request)
        {
            var result = await _itsmService.RecordSLABreach(slaId, request.BreachDetails, request.BreachTime);
            return Ok(result);
        }

        // Knowledge Management
        [HttpPost("knowledge/articles")]
        public async Task<IActionResult> CreateKnowledgeArticle([FromBody] CreateKnowledgeArticleDto request)
        {
            var article = new KnowledgeArticle
            {
                Title = request.Title,
                Content = request.Content,
                Summary = request.Summary,
                Category = request.Category,
                Tags = request.Tags
            };
            var result = await _itsmService.CreateKnowledgeArticle(article);
            return Ok(result);
        }

        [HttpGet("knowledge/articles")]
        public async Task<IActionResult> GetKnowledgeArticles([FromQuery] KnowledgeFilterDto filter)
        {
            var itsmFilter = filter != null ? new KnowledgeFilter
            {
                Category = filter.Category,
                Status = filter.Status,
                Tags = filter.Tags,
                Author = filter.Author,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate
            } : null;
            var articles = await _itsmService.GetKnowledgeArticles(itsmFilter);
            return Ok(articles);
        }

        [HttpPost("knowledge/articles/{articleId}/publish")]
        public async Task<IActionResult> PublishKnowledgeArticle(int articleId)
        {
            var article = await _itsmService.PublishKnowledgeArticle(articleId);
            return Ok(article);
        }

        // ITIL Compliance
        [HttpGet("itil/framework")]
        public async Task<IActionResult> GetITILFramework()
        {
            var framework = await _itilComplianceService.GetITILFramework();
            return Ok(framework);
        }

        [HttpGet("itil/processes/{processId}")]
        public async Task<IActionResult> GetProcessDefinition(string processId)
        {
            var process = await _itilComplianceService.GetProcessDefinition(processId);
            return Ok(process);
        }

        [HttpGet("itil/processes")]
        public async Task<IActionResult> GetAllProcessDefinitions()
        {
            var processes = await _itilComplianceService.GetAllProcessDefinitions();
            return Ok(processes);
        }

        [HttpPost("itil/processes/{processId}/instances")]
        public async Task<IActionResult> CreateProcessInstance(string processId, [FromBody] CreateProcessInstanceDto request)
        {
            var processData = new ITILProcessData
            {
                Properties = request.Properties
            };
            var instance = await _itilComplianceService.CreateProcessInstance(processId, processData);
            return Ok(instance);
        }

        [HttpPost("itil/compliance/reports")]
        public async Task<IActionResult> GenerateComplianceReport([FromBody] ComplianceReportRequestDto request)
        {
            var report = await _itilComplianceService.GenerateComplianceReport(request.StartDate, request.EndDate);
            return Ok(report);
        }

        [HttpPost("itil/audits")]
        public async Task<IActionResult> ConductITILAudit([FromBody] ConductAuditDto request)
        {
            var scope = new ITILAuditScope
            {
                Processes = request.Processes,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };
            var audit = await _itilComplianceService.ConductITILAudit(scope);
            return Ok(audit);
        }

        [HttpGet("itil/maturity")]
        public async Task<IActionResult> AssessITILMaturity()
        {
            var maturity = await _itilComplianceService.AssessITILMaturity();
            return Ok(maturity);
        }

        [HttpPost("itil/certification")]
        public async Task<IActionResult> PrepareForITILCertification([FromBody] CertificationRequestDto request)
        {
            var level = new ITILCertificationLevel();
            var certification = await _itilComplianceService.PrepareForITILCertification(level);
            return Ok(certification);
        }

        // Dashboard and Analytics
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetITSMDashboard()
        {
            var dashboard = new
            {
                Incidents = await _itsmService.GetIncidents(),
                Problems = await _itsmService.GetProblems(),
                Changes = await _itsmService.GetChangeRequests()
            };
            return Ok(dashboard);
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetITSMAnalytics([FromQuery] ITSMAnalyticsFilter filter)
        {
            var analytics = new
            {
                IncidentCount = (await _itsmService.GetIncidents()).Count,
                ProblemCount = (await _itsmService.GetProblems()).Count,
                ChangeCount = (await _itsmService.GetChangeRequests()).Count
            };
            return Ok(analytics);
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetITILMetrics([FromQuery] ITILMetricsFilter filter)
        {
            var metrics = await _itilComplianceService.GetRealTimeMetrics();
            return Ok(metrics);
        }
    }
}
