using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Application.Interfaces;
using System.Text.Json;

namespace HelpDeskSystem.ITSM.Services
{
    public interface IITSMService
    {
        // Incident Management
        Task<Incident> CreateIncident(Incident incident);
        Task<Incident> UpdateIncident(int incidentId, Incident incident);
        Task<List<Incident>> GetIncidents(IncidentFilter filter = null);
        Task<Incident> AssignIncident(int incidentId, string assigneeId, string assignmentGroup = null);
        Task<Incident> EscalateIncident(int incidentId, string reason, string escalationLevel);
        Task<Incident> ResolveIncident(int incidentId, string resolution, string resolutionCode);
        Task<Incident> CloseIncident(int incidentId, string closureCode, string satisfactionRating = null);

        // Problem Management
        Task<Problem> CreateProblem(Problem problem);
        Task<Problem> UpdateProblem(int problemId, Problem problem);
        Task<List<Problem>> GetProblems(ProblemFilter filter = null);
        Task<Problem> LinkIncidentToProblem(int incidentId, int problemId);
        Task<RootCauseAnalysis> PerformRootCauseAnalysis(int problemId, RootCauseAnalysis rca);
        Task<Problem> ImplementPermanentFix(int problemId, string fixDescription, DateTime implementationDate);

        // Change Management
        Task<ChangeRequest> CreateChangeRequest(ChangeRequest change);
        Task<ChangeRequest> UpdateChangeRequest(int changeId, ChangeRequest change);
        Task<List<ChangeRequest>> GetChangeRequests(ChangeFilter filter = null);
        Task<ChangeRequest> SubmitForApproval(int changeId, List<string> approvers);
        Task<ChangeRequest> ApproveChange(int changeId, string approverId, string comments);
        Task<ChangeRequest> RejectChange(int changeId, string approverId, string reason);
        Task<ChangeRequest> ScheduleChange(int changeId, DateTime scheduledDate, TimeSpan estimatedDuration);
        Task<ChangeRequest> ImplementChange(int changeId, string implementationDetails);
        Task<ChangeRequest> ReviewChange(int changeId, ChangeReview review);

        // Asset Management (CMDB)
        Task<ConfigurationItem> CreateConfigurationItem(ConfigurationItem ci);
        Task<ConfigurationItem> UpdateConfigurationItem(int ciId, ConfigurationItem ci);
        Task<List<ConfigurationItem>> GetConfigurationItems(CIFilter filter = null);
        Task<List<ConfigurationItem>> GetRelatedCIs(int ciId);
        Task<ConfigurationItem> LinkCIs(int sourceCiId, int targetCiId, string relationshipType);
        Task<AssetLifecycle> RecordAssetLifecycle(int ciId, AssetLifecycleEvent lifecycleEvent);

        // Service Catalog
        Task<ServiceCatalogItem> CreateServiceCatalogItem(ServiceCatalogItem service);
        Task<ServiceCatalogItem> UpdateServiceCatalogItem(int serviceId, ServiceCatalogItem service);
        Task<List<ServiceCatalogItem>> GetServiceCatalog(ServiceFilter filter = null);
        Task<ServiceRequest> CreateServiceRequest(ServiceRequest request);
        Task<ServiceRequest> UpdateServiceRequest(int requestId, ServiceRequest request);
        Task<List<ServiceRequest>> GetServiceRequests(ServiceRequestFilter filter = null);
        Task<ServiceRequest> FulfillServiceRequest(int requestId, string fulfillmentDetails);

        // SLA Management
        Task<ServiceLevelAgreement> CreateSLA(ServiceLevelAgreement sla);
        Task<ServiceLevelAgreement> UpdateSLA(int slaId, ServiceLevelAgreement sla);
        Task<List<ServiceLevelAgreement>> GetSLAs(SLAFilter filter = null);
        Task<SLABreach> RecordSLABreach(int slaId, string breachDetails, DateTime breachTime);
        Task<List<SLAMetric>> GetSLAMetrics(string serviceId, DateTime startDate, DateTime endDate);

        // Knowledge Management
        Task<KnowledgeArticle> CreateKnowledgeArticle(KnowledgeArticle article);
        Task<KnowledgeArticle> UpdateKnowledgeArticle(int articleId, KnowledgeArticle article);
        Task<List<KnowledgeArticle>> GetKnowledgeArticles(KnowledgeFilter filter = null);
        Task<KnowledgeArticle> PublishKnowledgeArticle(int articleId);
        Task<KnowledgeArticle> ArchiveKnowledgeArticle(int articleId);
        Task<List<KnowledgeArticle>> GetRelatedArticles(int articleId);
        Task<KnowledgeFeedback> RecordKnowledgeFeedback(int articleId, KnowledgeFeedback feedback);

        // Self-Service Portal
        Task<SelfServiceRequest> CreateSelfServiceRequest(SelfServiceRequest request);
        Task<List<SelfServiceRequest>> GetSelfServiceRequests(string userId);
        Task<ServiceRequestTemplate> CreateServiceRequestTemplate(ServiceRequestTemplate template);
        Task<List<ServiceRequestTemplate>> GetServiceRequestTemplates();
        Task<CatalogItem> GetCatalogItemDetails(int itemId);

        // ITIL Compliance
        Task<ITILComplianceReport> GenerateComplianceReport(DateTime startDate, DateTime endDate);
        Task<List<ITILProcess>> GetITILProcesses();
        Task<ITILAudit> PerformITILAudit(string processId);
        Task<bool> ValidateITILCompliance(string processType, object processData);
    }

    public class ITSMService : IITSMService
    {
        private readonly ILogger<ITSMService> _logger;
        private readonly ITicketService _ticketService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITSMSettings _settings;

        public ITSMService(
            ILogger<ITSMService> logger,
            ITicketService ticketService,
            IUnitOfWork unitOfWork,
            ITSMSettings settings)
        {
            _logger = logger;
            _ticketService = ticketService;
            _unitOfWork = unitOfWork;
            _settings = settings;
        }

        #region Incident Management

        public async Task<Incident> CreateIncident(Incident incident)
        {
            try
            {
                // Generate incident number
                incident.IncidentNumber = await GenerateIncidentNumber();
                incident.CreatedAt = DateTime.UtcNow;
                incident.Status = IncidentStatus.New;

                // Check for SLA
                await AssignIncidentSLA(incident);

                // Create incident ticket
                var ticket = new Ticket
                {
                    Title = incident.Title,
                    Description = incident.Description,
                    PriorityId = incident.PriorityId,
                    CategoryId = incident.CategoryId,
                    StatusId = 1, // New
                    CreatedAt = DateTime.UtcNow,
                    TicketNumber = incident.IncidentNumber,
                    Type = TicketType.Incident
                };

                var createdTicket = await _ticketService.CreateTicketAsync(ticket);
                incident.TicketId = createdTicket.Id;

                // Log incident creation
                _logger.LogInformation("Created incident {IncidentNumber} for user {UserId}", 
                    incident.IncidentNumber, incident.ReportedByUserId);

                return incident;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating incident");
                throw;
            }
        }

        public async Task<Incident> UpdateIncident(int incidentId, Incident incident)
        {
            try
            {
                var existingIncident = await GetIncidentById(incidentId);
                if (existingIncident == null)
                    throw new ArgumentException($"Incident {incidentId} not found");

                // Update fields
                existingIncident.Title = incident.Title;
                existingIncident.Description = incident.Description;
                existingIncident.PriorityId = incident.PriorityId;
                existingIncident.CategoryId = incident.CategoryId;
                existingIncident.Impact = incident.Impact;
                existingIncident.Urgency = incident.Urgency;
                existingIncident.UpdatedAt = DateTime.UtcNow;

                // Recalculate priority based on impact and urgency
                existingIncident.PriorityId = CalculatePriority(incident.Impact, incident.Urgency);

                // Update corresponding ticket
                await _ticketService.UpdateTicketAsync(new Ticket
                {
                    Id = existingIncident.TicketId,
                    Title = existingIncident.Title,
                    Description = existingIncident.Description,
                    PriorityId = existingIncident.PriorityId,
                    CategoryId = existingIncident.CategoryId
                });

                return existingIncident;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating incident {IncidentId}", incidentId);
                throw;
            }
        }

        public async Task<List<Incident>> GetIncidents(IncidentFilter filter = null)
        {
            try
            {
                var tickets = await _ticketService.GetTicketsAsync();
                var incidents = tickets.Where(t => t.Type == TicketType.Incident)
                    .Select(t => new Incident
                    {
                        Id = t.Id,
                        IncidentNumber = t.TicketNumber,
                        Title = t.Title,
                        Description = t.Description,
                        PriorityId = t.PriorityId,
                        CategoryId = t.CategoryId,
                        Status = MapTicketStatusToIncidentStatus(t.Status),
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        AssignedToUserId = t.AssignedToUserId?.ToString(),
                        ReportedByUserId = t.CreatedByUserId?.ToString(),
                        TicketId = t.Id
                    }).ToList();

                // Apply filters
                if (filter != null)
                {
                    if (!string.IsNullOrEmpty(filter.Status))
                        incidents = incidents.Where(i => i.Status.ToString() == filter.Status).ToList();
                    
                    if (!string.IsNullOrEmpty(filter.Priority))
                        incidents = incidents.Where(i => i.PriorityId.ToString() == filter.Priority).ToList();
                    
                    if (!string.IsNullOrEmpty(filter.AssignedTo))
                        incidents = incidents.Where(i => i.AssignedToUserId == filter.AssignedTo).ToList();
                    
                    if (filter.StartDate.HasValue)
                        incidents = incidents.Where(i => i.CreatedAt >= filter.StartDate.Value).ToList();
                    
                    if (filter.EndDate.HasValue)
                        incidents = incidents.Where(i => i.CreatedAt <= filter.EndDate.Value).ToList();
                }

                return incidents.OrderByDescending(i => i.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching incidents");
                return new List<Incident>();
            }
        }

        public async Task<Incident> AssignIncident(int incidentId, string assigneeId, string assignmentGroup = null)
        {
            try
            {
                var incident = await GetIncidentById(incidentId);
                if (incident == null)
                    throw new ArgumentException($"Incident {incidentId} not found");

                incident.AssignedToUserId = assigneeId;
                incident.AssignmentGroup = assignmentGroup;
                incident.Status = IncidentStatus.InProgress;
                incident.AssignedAt = DateTime.UtcNow;

                // Update ticket assignment
                await _ticketService.AssignTicketAsync(incident.TicketId, int.Parse(assigneeId));

                // Send notification
                await SendAssignmentNotification(incident, assigneeId);

                _logger.LogInformation("Assigned incident {IncidentNumber} to {AssigneeId}", 
                    incident.IncidentNumber, assigneeId);

                return incident;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning incident {IncidentId}", incidentId);
                throw;
            }
        }

        public async Task<Incident> EscalateIncident(int incidentId, string reason, string escalationLevel)
        {
            try
            {
                var incident = await GetIncidentById(incidentId);
                if (incident == null)
                    throw new ArgumentException($"Incident {incidentId} not found");

                incident.EscalationLevel = escalationLevel;
                incident.EscalationReason = reason;
                incident.EscalatedAt = DateTime.UtcNow;
                incident.Status = IncidentStatus.Escalated;

                // Update priority based on escalation
                incident.PriorityId = Math.Max(1, incident.PriorityId - 1); // Higher priority

                // Send escalation notification
                await SendEscalationNotification(incident, reason);

                _logger.LogInformation("Escalated incident {IncidentNumber} to level {EscalationLevel}", 
                    incident.IncidentNumber, escalationLevel);

                return incident;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalating incident {IncidentId}", incidentId);
                throw;
            }
        }

        public async Task<Incident> ResolveIncident(int incidentId, string resolution, string resolutionCode)
        {
            try
            {
                var incident = await GetIncidentById(incidentId);
                if (incident == null)
                    throw new ArgumentException($"Incident {incidentId} not found");

                incident.Resolution = resolution;
                incident.ResolutionCode = resolutionCode;
                incident.ResolvedAt = DateTime.UtcNow;
                incident.Status = IncidentStatus.Resolved;

                // Update ticket status
                await _ticketService.UpdateTicketStatusAsync(incident.TicketId, "Resolved");

                // Check if related problem exists and update
                await UpdateRelatedProblem(incident);

                _logger.LogInformation("Resolved incident {IncidentNumber} with code {ResolutionCode}", 
                    incident.IncidentNumber, resolutionCode);

                return incident;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving incident {IncidentId}", incidentId);
                throw;
            }
        }

        public async Task<Incident> CloseIncident(int incidentId, string closureCode, string satisfactionRating = null)
        {
            try
            {
                var incident = await GetIncidentById(incidentId);
                if (incident == null)
                    throw new ArgumentException($"Incident {incidentId} not found");

                incident.ClosureCode = closureCode;
                incident.SatisfactionRating = satisfactionRating;
                incident.ClosedAt = DateTime.UtcNow;
                incident.Status = IncidentStatus.Closed;

                // Update ticket status
                await _ticketService.UpdateTicketStatusAsync(incident.TicketId, "Closed");

                // Generate closure report
                await GenerateClosureReport(incident);

                _logger.LogInformation("Closed incident {IncidentNumber} with code {ClosureCode}", 
                    incident.IncidentNumber, closureCode);

                return incident;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing incident {IncidentId}", incidentId);
                throw;
            }
        }

        #endregion

        #region Problem Management

        public async Task<Problem> CreateProblem(Problem problem)
        {
            try
            {
                problem.ProblemNumber = await GenerateProblemNumber();
                problem.CreatedAt = DateTime.UtcNow;
                problem.Status = ProblemStatus.New;

                // Create problem ticket
                var ticket = new Ticket
                {
                    Title = problem.Title,
                    Description = problem.Description,
                    PriorityId = problem.PriorityId,
                    CategoryId = problem.CategoryId,
                    StatusId = 1, // New
                    CreatedAt = DateTime.UtcNow,
                    TicketNumber = problem.ProblemNumber,
                    Type = TicketType.Problem
                };

                var createdTicket = await _ticketService.CreateTicketAsync(ticket);
                problem.TicketId = createdTicket.Id;

                _logger.LogInformation("Created problem {ProblemNumber}", problem.ProblemNumber);
                return problem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating problem");
                throw;
            }
        }

        public async Task<Problem> LinkIncidentToProblem(int incidentId, int problemId)
        {
            try
            {
                var incident = await GetIncidentById(incidentId);
                var problem = await GetProblemById(problemId);

                if (incident != null && problem != null)
                {
                    incident.ProblemId = problemId;
                    problem.RelatedIncidents.Add(incidentId);

                    _logger.LogInformation("Linked incident {IncidentId} to problem {ProblemId}", incidentId, problemId);
                }

                return problem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking incident {IncidentId} to problem {ProblemId}", incidentId, problemId);
                throw;
            }
        }

        public async Task<RootCauseAnalysis> PerformRootCauseAnalysis(int problemId, RootCauseAnalysis rca)
        {
            try
            {
                rca.ProblemId = problemId;
                rca.PerformedAt = DateTime.UtcNow;
                rca.Status = RCAStatus.Completed;

                // Analyze incident patterns
                var relatedIncidents = await GetIncidentsForProblem(problemId);
                rca.IncidentPatterns = AnalyzeIncidentPatterns(relatedIncidents);

                // Generate recommendations
                rca.Recommendations = GenerateRecommendations(rca);

                _logger.LogInformation("Completed RCA for problem {ProblemId}", problemId);
                return rca;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing RCA for problem {ProblemId}", problemId);
                throw;
            }
        }

        #endregion

        #region Change Management

        public async Task<ChangeRequest> CreateChangeRequest(ChangeRequest change)
        {
            try
            {
                change.ChangeNumber = await GenerateChangeNumber();
                change.CreatedAt = DateTime.UtcNow;
                change.Status = ChangeStatus.New;

                // Assess change risk
                change.RiskAssessment = await AssessChangeRisk(change);
                change.PriorityId = CalculateChangePriority(change);

                // Create change ticket
                var ticket = new Ticket
                {
                    Title = change.Title,
                    Description = change.Description,
                    PriorityId = change.PriorityId,
                    CategoryId = change.CategoryId,
                    StatusId = 1, // New
                    CreatedAt = DateTime.UtcNow,
                    TicketNumber = change.ChangeNumber,
                    Type = TicketType.Change
                };

                var createdTicket = await _ticketService.CreateTicketAsync(ticket);
                change.TicketId = createdTicket.Id;

                _logger.LogInformation("Created change request {ChangeNumber}", change.ChangeNumber);
                return change;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating change request");
                throw;
            }
        }

        public async Task<ChangeRequest> SubmitForApproval(int changeId, List<string> approvers)
        {
            try
            {
                var change = await GetChangeRequestById(changeId);
                if (change == null)
                    throw new ArgumentException($"Change request {changeId} not found");

                change.Status = ChangeStatus.PendingApproval;
                change.Approvers = approvers;
                change.SubmittedForApprovalAt = DateTime.UtcNow;

                // Send approval requests
                await SendApprovalRequests(change, approvers);

                _logger.LogInformation("Submitted change {ChangeNumber} for approval", change.ChangeNumber);
                return change;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting change {ChangeId} for approval", changeId);
                throw;
            }
        }

        #endregion

        #region Asset Management (CMDB)

        public async Task<ConfigurationItem> CreateConfigurationItem(ConfigurationItem ci)
        {
            try
            {
                ci.CINumber = await GenerateCINumber();
                ci.CreatedAt = DateTime.UtcNow;
                ci.Status = CIStatus.Active;

                // Validate CI relationships
                await ValidateCIRelationships(ci);

                _logger.LogInformation("Created CI {CINumber} of type {CIType}", ci.CINumber, ci.CIType);
                return ci;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating CI");
                throw;
            }
        }

        public async Task<List<ConfigurationItem>> GetRelatedCIs(int ciId)
        {
            try
            {
                // Implementation to get related CIs based on relationships
                var relatedCIs = new List<ConfigurationItem>();
                
                // This would query the CMDB for related items
                // based on dependency, containment, or connection relationships

                return relatedCIs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related CIs for {CIId}", ciId);
                return new List<ConfigurationItem>();
            }
        }

        #endregion

        #region Service Catalog

        public async Task<ServiceCatalogItem> CreateServiceCatalogItem(ServiceCatalogItem service)
        {
            try
            {
                service.ServiceId = await GenerateServiceId();
                service.CreatedAt = DateTime.UtcNow;
                service.Status = ServiceStatus.Active;

                // Define SLA for service
                service.SLA = await DefineServiceSLA(service);

                _logger.LogInformation("Created service catalog item {ServiceId}", service.ServiceId);
                return service;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service catalog item");
                throw;
            }
        }

        #endregion

        #region SLA Management

        public async Task<ServiceLevelAgreement> CreateSLA(ServiceLevelAgreement sla)
        {
            try
            {
                sla.SLAId = await GenerateSLAId();
                sla.CreatedAt = DateTime.UtcNow;
                sla.Status = SLAStatus.Active;

                // Calculate breach penalties
                sla.BreachPenalties = await CalculateBreachPenalties(sla);

                _logger.LogInformation("Created SLA {SLAId} for service {ServiceId}", sla.SLAId, sla.ServiceId);
                return sla;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SLA");
                throw;
            }
        }

        public async Task<SLABreach> RecordSLABreach(int slaId, string breachDetails, DateTime breachTime)
        {
            try
            {
                var breach = new SLABreach
                {
                    SLAId = slaId,
                    BreachDetails = breachDetails,
                    BreachTime = breachTime,
                    RecordedAt = DateTime.UtcNow,
                    Status = BreachStatus.Open
                };

                // Trigger breach notifications
                await TriggerBreachNotifications(breach);

                // Initiate breach resolution workflow
                await InitiateBreachResolution(breach);

                _logger.LogInformation("Recorded SLA breach for SLA {SLAId}", slaId);
                return breach;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording SLA breach for SLA {SLAId}", slaId);
                throw;
            }
        }

        #endregion

        #region Knowledge Management

        public async Task<KnowledgeArticle> CreateKnowledgeArticle(KnowledgeArticle article)
        {
            try
            {
                article.ArticleNumber = await GenerateArticleNumber();
                article.CreatedAt = DateTime.UtcNow;
                article.Status = ArticleStatus.Draft;

                // Analyze article for SEO and searchability
                await OptimizeArticleForSearch(article);

                _logger.LogInformation("Created knowledge article {ArticleNumber}", article.ArticleNumber);
                return article;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating knowledge article");
                throw;
            }
        }

        public async Task<KnowledgeArticle> PublishKnowledgeArticle(int articleId)
        {
            try
            {
                var article = await GetKnowledgeArticleById(articleId);
                if (article == null)
                    throw new ArgumentException($"Knowledge article {articleId} not found");

                article.Status = ArticleStatus.Published;
                article.PublishedAt = DateTime.UtcNow;

                // Update search index
                await UpdateSearchIndex(article);

                // Notify subscribers
                await NotifyArticleSubscribers(article);

                _logger.LogInformation("Published knowledge article {ArticleNumber}", article.ArticleNumber);
                return article;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing knowledge article {ArticleId}", articleId);
                throw;
            }
        }

        #endregion

        #region Self-Service Portal

        public async Task<SelfServiceRequest> CreateSelfServiceRequest(SelfServiceRequest request)
        {
            try
            {
                request.RequestNumber = await GenerateSelfServiceRequestNumber();
                request.CreatedAt = DateTime.UtcNow;
                request.Status = SelfServiceStatus.New;

                // Check approval requirements
                if (await RequiresApproval(request))
                {
                    request.Status = SelfServiceStatus.PendingApproval;
                    await SendApprovalRequest(request);
                }
                else
                {
                    // Auto-fulfill if possible
                    await AutoFulfillRequest(request);
                }

                _logger.LogInformation("Created self-service request {RequestNumber}", request.RequestNumber);
                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating self-service request");
                throw;
            }
        }

        #endregion

        #region ITIL Compliance

        public async Task<ITILComplianceReport> GenerateComplianceReport(DateTime startDate, DateTime endDate)
        {
            try
            {
                var report = new ITILComplianceReport
                {
                    ReportPeriod = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                    GeneratedAt = DateTime.UtcNow
                };

                // Incident Management Compliance
                report.IncidentManagementCompliance = await CalculateIncidentCompliance(startDate, endDate);

                // Problem Management Compliance
                report.ProblemManagementCompliance = await CalculateProblemCompliance(startDate, endDate);

                // Change Management Compliance
                report.ChangeManagementCompliance = await CalculateChangeCompliance(startDate, endDate);

                // Asset Management Compliance
                report.AssetManagementCompliance = await CalculateAssetCompliance(startDate, endDate);

                // Overall Compliance Score
                report.OverallComplianceScore = CalculateOverallCompliance(report);

                _logger.LogInformation("Generated ITIL compliance report for period {StartDate} to {EndDate}", 
                    startDate, endDate);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ITIL compliance report");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private async Task<string> GenerateIncidentNumber()
        {
            var prefix = "INC";
            var date = DateTime.Now.ToString("yyyyMMdd");
            var sequence = await GetNextSequence("Incident");
            return $"{prefix}{date}{sequence:D4}";
        }

        private async Task<string> GenerateProblemNumber()
        {
            var prefix = "PRB";
            var date = DateTime.Now.ToString("yyyyMMdd");
            var sequence = await GetNextSequence("Problem");
            return $"{prefix}{date}{sequence:D4}";
        }

        private async Task<string> GenerateChangeNumber()
        {
            var prefix = "CHG";
            var date = DateTime.Now.ToString("yyyyMMdd");
            var sequence = await GetNextSequence("Change");
            return $"{prefix}{date}{sequence:D4}";
        }

        private async Task<string> GenerateCINumber()
        {
            var prefix = "CI";
            var date = DateTime.Now.ToString("yyyyMMdd");
            var sequence = await GetNextSequence("CI");
            return $"{prefix}{date}{sequence:D4}";
        }

        private async Task<string> GenerateServiceId()
        {
            var prefix = "SVC";
            var date = DateTime.Now.ToString("yyyyMMdd");
            var sequence = await GetNextSequence("Service");
            return $"{prefix}{date}{sequence:D4}";
        }

        private async Task<string> GenerateSLAId()
        {
            var prefix = "SLA";
            var date = DateTime.Now.ToString("yyyyMMdd");
            var sequence = await GetNextSequence("SLA");
            return $"{prefix}{date}{sequence:D4}";
        }

        private async Task<string> GenerateArticleNumber()
        {
            var prefix = "KB";
            var date = DateTime.Now.ToString("yyyyMMdd");
            var sequence = await GetNextSequence("KB");
            return $"{prefix}{date}{sequence:D4}";
        }

        private async Task<string> GenerateSelfServiceRequestNumber()
        {
            var prefix = "SR";
            var date = DateTime.Now.ToString("yyyyMMdd");
            var sequence = await GetNextSequence("SelfService");
            return $"{prefix}{date}{sequence:D4}";
        }

        private async Task<int> GetNextSequence(string sequenceType)
        {
            // Implementation to get next sequence number from database
            return 1; // Placeholder
        }

        private int CalculatePriority(string impact, string urgency)
        {
            var impactValue = impact switch
            {
                "High" => 3,
                "Medium" => 2,
                "Low" => 1,
                _ => 2
            };

            var urgencyValue = urgency switch
            {
                "High" => 3,
                "Medium" => 2,
                "Low" => 1,
                _ => 2
            };

            return (impactValue + urgencyValue) / 2;
        }

        private IncidentStatus MapTicketStatusToIncidentStatus(string ticketStatus)
        {
            return ticketStatus switch
            {
                "New" => IncidentStatus.New,
                "InProgress" => IncidentStatus.InProgress,
                "Waiting" => IncidentStatus.Waiting,
                "Resolved" => IncidentStatus.Resolved,
                "Closed" => IncidentStatus.Closed,
                "Reopened" => IncidentStatus.Reopened,
                _ => IncidentStatus.New
            };
        }

        private async Task<Incident> GetIncidentById(int incidentId)
        {
            var incidents = await GetIncidents();
            return incidents.FirstOrDefault(i => i.Id == incidentId);
        }

        private async Task<Problem> GetProblemById(int problemId)
        {
            // Implementation to get problem by ID
            return new Problem(); // Placeholder
        }

        private async Task<ChangeRequest> GetChangeRequestById(int changeId)
        {
            // Implementation to get change request by ID
            return new ChangeRequest(); // Placeholder
        }

        private async Task<KnowledgeArticle> GetKnowledgeArticleById(int articleId)
        {
            // Implementation to get knowledge article by ID
            return new KnowledgeArticle(); // Placeholder
        }

        private async Task<List<Incident>> GetIncidentsForProblem(int problemId)
        {
            // Implementation to get incidents linked to a problem
            return new List<Incident>(); // Placeholder
        }

        private async Task AssignIncidentSLA(Incident incident)
        {
            // Implementation to assign appropriate SLA based on incident properties
        }

        private async Task SendAssignmentNotification(Incident incident, string assigneeId)
        {
            // Implementation to send assignment notification
        }

        private async Task SendEscalationNotification(Incident incident, string reason)
        {
            // Implementation to send escalation notification
        }

        private async Task UpdateRelatedProblem(Incident incident)
        {
            // Implementation to update related problem when incident is resolved
        }

        private async Task GenerateClosureReport(Incident incident)
        {
            // Implementation to generate incident closure report
        }

        private List<string> AnalyzeIncidentPatterns(List<Incident> incidents)
        {
            // Implementation to analyze patterns in related incidents
            return new List<string>(); // Placeholder
        }

        private List<string> GenerateRecommendations(RootCauseAnalysis rca)
        {
            // Implementation to generate recommendations based on RCA
            return new List<string>(); // Placeholder
        }

        private async Task<string> AssessChangeRisk(ChangeRequest change)
        {
            // Implementation to assess change risk
            return "Medium"; // Placeholder
        }

        private int CalculateChangePriority(ChangeRequest change)
        {
            // Implementation to calculate change priority based on risk and impact
            return 2; // Placeholder
        }

        private async Task SendApprovalRequests(ChangeRequest change, List<string> approvers)
        {
            // Implementation to send approval requests
        }

        private async Task ValidateCIRelationships(ConfigurationItem ci)
        {
            // Implementation to validate CI relationships
        }

        private async Task<ServiceLevelAgreement> DefineServiceSLA(ServiceCatalogItem service)
        {
            // Implementation to define SLA for service
            return new ServiceLevelAgreement(); // Placeholder
        }

        private async Task<List<BreachPenalty>> CalculateBreachPenalties(ServiceLevelAgreement sla)
        {
            // Implementation to calculate breach penalties
            return new List<BreachPenalty>(); // Placeholder
        }

        private async Task TriggerBreachNotifications(SLABreach breach)
        {
            // Implementation to trigger breach notifications
        }

        private async Task InitiateBreachResolution(SLABreach breach)
        {
            // Implementation to initiate breach resolution workflow
        }

        private async Task OptimizeArticleForSearch(KnowledgeArticle article)
        {
            // Implementation to optimize article for search
        }

        private async Task UpdateSearchIndex(KnowledgeArticle article)
        {
            // Implementation to update search index
        }

        private async Task NotifyArticleSubscribers(KnowledgeArticle article)
        {
            // Implementation to notify article subscribers
        }

        private async Task<bool> RequiresApproval(SelfServiceRequest request)
        {
            // Implementation to check if request requires approval
            return false; // Placeholder
        }

        private async Task SendApprovalRequest(SelfServiceRequest request)
        {
            // Implementation to send approval request for self-service
        }

        private async Task AutoFulfillRequest(SelfServiceRequest request)
        {
            // Implementation to auto-fulfill self-service request
        }

        private async Task<double> CalculateIncidentCompliance(DateTime startDate, DateTime endDate)
        {
            // Implementation to calculate incident management compliance
            return 95.5; // Placeholder
        }

        private async Task<double> CalculateProblemCompliance(DateTime startDate, DateTime endDate)
        {
            // Implementation to calculate problem management compliance
            return 92.3; // Placeholder
        }

        private async Task<double> CalculateChangeCompliance(DateTime startDate, DateTime endDate)
        {
            // Implementation to calculate change management compliance
            return 88.7; // Placeholder
        }

        private async Task<double> CalculateAssetCompliance(DateTime startDate, DateTime endDate)
        {
            // Implementation to calculate asset management compliance
            return 94.1; // Placeholder
        }

        private double CalculateOverallCompliance(ITILComplianceReport report)
        {
            return (report.IncidentManagementCompliance + 
                    report.ProblemManagementCompliance + 
                    report.ChangeManagementCompliance + 
                    report.AssetManagementCompliance) / 4.0;
        }

        #endregion

        // Placeholder implementations for remaining interface methods
        public Task<Problem> UpdateProblem(int problemId, Problem problem) => Task.FromResult(new Problem());
        public Task<List<Problem>> GetProblems(ProblemFilter filter = null) => Task.FromResult(new List<Problem>());
        public Task<Problem> ImplementPermanentFix(int problemId, string fixDescription, DateTime implementationDate) => Task.FromResult(new Problem());
        public Task<ChangeRequest> UpdateChangeRequest(int changeId, ChangeRequest change) => Task.FromResult(new ChangeRequest());
        public Task<List<ChangeRequest>> GetChangeRequests(ChangeFilter filter = null) => Task.FromResult(new List<ChangeRequest>());
        public Task<ChangeRequest> ApproveChange(int changeId, string approverId, string comments) => Task.FromResult(new ChangeRequest());
        public Task<ChangeRequest> RejectChange(int changeId, string approverId, string reason) => Task.FromResult(new ChangeRequest());
        public Task<ChangeRequest> ScheduleChange(int changeId, DateTime scheduledDate, TimeSpan estimatedDuration) => Task.FromResult(new ChangeRequest());
        public Task<ChangeRequest> ImplementChange(int changeId, string implementationDetails) => Task.FromResult(new ChangeRequest());
        public Task<ChangeRequest> ReviewChange(int changeId, ChangeReview review) => Task.FromResult(new ChangeRequest());
        public Task<ConfigurationItem> UpdateConfigurationItem(int ciId, ConfigurationItem ci) => Task.FromResult(new ConfigurationItem());
        public Task<List<ConfigurationItem>> GetConfigurationItems(CIFilter filter = null) => Task.FromResult(new List<ConfigurationItem>());
        public Task<ConfigurationItem> LinkCIs(int sourceCiId, int targetCiId, string relationshipType) => Task.FromResult(new ConfigurationItem());
        public Task<AssetLifecycle> RecordAssetLifecycle(int ciId, AssetLifecycleEvent lifecycleEvent) => Task.FromResult(new AssetLifecycle());
        public Task<ServiceCatalogItem> UpdateServiceCatalogItem(int serviceId, ServiceCatalogItem service) => Task.FromResult(new ServiceCatalogItem());
        public Task<List<ServiceCatalogItem>> GetServiceCatalog(ServiceFilter filter = null) => Task.FromResult(new List<ServiceCatalogItem>());
        public Task<ServiceRequest> CreateServiceRequest(ServiceRequest request) => Task.FromResult(new ServiceRequest());
        public Task<ServiceRequest> UpdateServiceRequest(int requestId, ServiceRequest request) => Task.FromResult(new ServiceRequest());
        public Task<List<ServiceRequest>> GetServiceRequests(ServiceRequestFilter filter = null) => Task.FromResult(new List<ServiceRequest>());
        public Task<ServiceRequest> FulfillServiceRequest(int requestId, string fulfillmentDetails) => Task.FromResult(new ServiceRequest());
        public Task<List<SelfServiceRequest>> GetSelfServiceRequests(string userId) => Task.FromResult(new List<SelfServiceRequest>());
        public Task<ServiceRequestTemplate> CreateServiceRequestTemplate(ServiceRequestTemplate template) => Task.FromResult(new ServiceRequestTemplate());
        public Task<List<ServiceRequestTemplate>> GetServiceRequestTemplates() => Task.FromResult(new List<ServiceRequestTemplate>());
        public Task<CatalogItem> GetCatalogItemDetails(int itemId) => Task.FromResult(new CatalogItem());
        public Task<KnowledgeArticle> UpdateKnowledgeArticle(int articleId, KnowledgeArticle article) => Task.FromResult(new KnowledgeArticle());
        public Task<List<KnowledgeArticle>> GetKnowledgeArticles(KnowledgeFilter filter = null) => Task.FromResult(new List<KnowledgeArticle>());
        public Task<KnowledgeArticle> ArchiveKnowledgeArticle(int articleId) => Task.FromResult(new KnowledgeArticle());
        public Task<List<KnowledgeArticle>> GetRelatedArticles(int articleId) => Task.FromResult(new List<KnowledgeArticle>());
        public Task<KnowledgeFeedback> RecordKnowledgeFeedback(int articleId, KnowledgeFeedback feedback) => Task.FromResult(new KnowledgeFeedback());
        public Task<ServiceLevelAgreement> UpdateSLA(int slaId, ServiceLevelAgreement sla) => Task.FromResult(new ServiceLevelAgreement());
        public Task<List<ServiceLevelAgreement>> GetSLAs(SLAFilter filter = null) => Task.FromResult(new List<ServiceLevelAgreement>());
        public Task<List<SLAMetric>> GetSLAMetrics(string serviceId, DateTime startDate, DateTime endDate) => Task.FromResult(new List<SLAMetric>());
        public Task<List<ITILProcess>> GetITILProcesses() => Task.FromResult(new List<ITILProcess>());
        public Task<ITILAudit> PerformITILAudit(string processId) => Task.FromResult(new ITILAudit());
        public Task<bool> ValidateITILCompliance(string processType, object processData) => Task.FromResult(true);
    }

    #region Data Models

    public class ITSMSettings
    {
        public bool EnableITILCompliance { get; set; } = true;
        public string DefaultSLAPolicy { get; set; }
        public List<string> RequiredApprovalGroups { get; set; } = new List<string>();
        public bool EnableAutoAssignment { get; set; } = true;
        public bool EnableKnowledgeBase { get; set; } = true;
        public bool EnableSelfService { get; set; } = true;
    }

    // Incident Management Models
    public class Incident
    {
        public int Id { get; set; }
        public string IncidentNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public IncidentStatus Status { get; set; }
        public string Impact { get; set; }
        public string Urgency { get; set; }
        public string AssignedToUserId { get; set; }
        public string AssignmentGroup { get; set; }
        public string ReportedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string Resolution { get; set; }
        public string ResolutionCode { get; set; }
        public string ClosureCode { get; set; }
        public string SatisfactionRating { get; set; }
        public string EscalationLevel { get; set; }
        public string EscalationReason { get; set; }
        public DateTime? EscalatedAt { get; set; }
        public int? ProblemId { get; set; }
        public int TicketId { get; set; }
    }

    public enum IncidentStatus
    {
        New,
        InProgress,
        Waiting,
        Resolved,
        Closed,
        Reopened,
        Escalated
    }

    public class IncidentFilter
    {
        public string Status { get; set; }
        public string Priority { get; set; }
        public string AssignedTo { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Category { get; set; }
    }

    // Problem Management Models
    public class Problem
    {
        public int Id { get; set; }
        public string ProblemNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public ProblemStatus Status { get; set; }
        public string AssignedToUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string RootCause { get; set; }
        public string PermanentFix { get; set; }
        public DateTime? FixedAt { get; set; }
        public List<int> RelatedIncidents { get; set; } = new List<int>();
        public int TicketId { get; set; }
    }

    public enum ProblemStatus
    {
        New,
        InProgress,
        Analysis,
        Fixed,
        Closed
    }

    public class ProblemFilter
    {
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class RootCauseAnalysis
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public string AnalysisMethod { get; set; }
        public string RootCause { get; set; }
        public List<string> ContributingFactors { get; set; } = new List<string>();
        public List<string> IncidentPatterns { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public RCAStatus Status { get; set; }
        public DateTime PerformedAt { get; set; }
        public string PerformedBy { get; set; }
    }

    public enum RCAStatus
    {
        InProgress,
        Completed,
        Reviewed
    }

    // Change Management Models
    public class ChangeRequest
    {
        public int Id { get; set; }
        public string ChangeNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int PriorityId { get; set; }
        public int CategoryId { get; set; }
        public ChangeStatus Status { get; set; }
        public ChangeType Type { get; set; }
        public string RiskAssessment { get; set; }
        public string ImpactAssessment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ScheduledStartDate { get; set; }
        public DateTime? ScheduledEndDate { get; set; }
        public DateTime? ImplementedAt { get; set; }
        public string RequestedByUserId { get; set; }
        public string AssignedToUserId { get; set; }
        public List<string> Approvers { get; set; } = new List<string>();
        public List<ChangeApproval> Approvals { get; set; } = new List<ChangeApproval>();
        public DateTime? SubmittedForApprovalAt { get; set; }
        public string ImplementationDetails { get; set; }
        public string BackoutPlan { get; set; }
        public string TestPlan { get; set; }
        public List<string> AffectedCIs { get; set; } = new List<string>();
        public int TicketId { get; set; }
    }

    public enum ChangeStatus
    {
        New,
        PendingApproval,
        Approved,
        Rejected,
        Scheduled,
        InProgress,
        Implemented,
        Reviewed,
        Cancelled
    }

    public enum ChangeType
    {
        Standard,
        Normal,
        Emergency
    }

    public class ChangeApproval
    {
        public string ApproverId { get; set; }
        public string Decision { get; set; }
        public string Comments { get; set; }
        public DateTime ApprovedAt { get; set; }
    }

    public class ChangeFilter
    {
        public string Status { get; set; }
        public string Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ChangeReview
    {
        public string ReviewerId { get; set; }
        public bool Successful { get; set; }
        public string Comments { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public DateTime ReviewedAt { get; set; }
    }

    // Asset Management Models
    public class ConfigurationItem
    {
        public int Id { get; set; }
        public string CINumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CIType { get; set; }
        public CIStatus Status { get; set; }
        public string Owner { get; set; }
        public string Location { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastScanned { get; set; }
        public List<CIRelationship> Relationships { get; set; } = new List<CIRelationship>();
    }

    public enum CIStatus
    {
        Active,
        Inactive,
        UnderMaintenance,
        Retired,
        Disposed
    }

    public class CIRelationship
    {
        public string SourceCIId { get; set; }
        public string TargetCIId { get; set; }
        public string RelationshipType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CIFilter
    {
        public string CIType { get; set; }
        public string Status { get; set; }
        public string Owner { get; set; }
        public string Location { get; set; }
    }

    public class AssetLifecycle
    {
        public int CIId { get; set; }
        public AssetLifecycleEvent Event { get; set; }
        public DateTime EventDate { get; set; }
        public string Description { get; set; }
        public string PerformedBy { get; set; }
    }

    public enum AssetLifecycleEvent
    {
        Procured,
        Deployed,
        UnderMaintenance,
        Upgraded,
        Retired,
        Disposed
    }

    // Service Catalog Models
    public class ServiceCatalogItem
    {
        public int Id { get; set; }
        public string ServiceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public ServiceStatus Status { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; }
        public ServiceLevelAgreement SLA { get; set; }
        public List<ServiceRequestTemplate> RequestTemplates { get; set; } = new List<ServiceRequestTemplate>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum ServiceStatus
    {
        Active,
        Inactive,
        UnderReview
    }

    public class ServiceFilter
    {
        public string Category { get; set; }
        public string Status { get; set; }
        public decimal? MinCost { get; set; }
        public decimal? MaxCost { get; set; }
    }

    public class ServiceRequest
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; }
        public string ServiceId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public RequestStatus Status { get; set; }
        public string RequestedByUserId { get; set; }
        public string AssignedToUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? FulfilledAt { get; set; }
        public string FulfillmentDetails { get; set; }
        public Dictionary<string, object> RequestData { get; set; } = new Dictionary<string, object>();
    }

    public enum RequestStatus
    {
        New,
        InProgress,
        Fulfilled,
        Cancelled,
        Rejected
    }

    public class ServiceRequestFilter
    {
        public string ServiceId { get; set; }
        public string Status { get; set; }
        public string RequestedBy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ServiceRequestTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TemplateField> Fields { get; set; } = new List<TemplateField>();
        public bool RequiresApproval { get; set; }
        public List<string> ApprovalGroups { get; set; } = new List<string>();
    }

    public class TemplateField
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string DefaultValue { get; set; }
        public List<string> Options { get; set; } = new List<string>();
    }

    public class CatalogItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public bool Available { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    // SLA Management Models
    public class ServiceLevelAgreement
    {
        public int Id { get; set; }
        public string SLAId { get; set; }
        public string Name { get; set; }
        public string ServiceId { get; set; }
        public SLAStatus Status { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public TimeSpan ResolutionTime { get; set; }
        public decimal AvailabilityPercentage { get; set; }
        public List<BreachPenalty> BreachPenalties { get; set; } = new List<BreachPenalty>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public enum SLAStatus
    {
        Active,
        Inactive,
        Draft
    }

    public class SLAFilter
    {
        public string ServiceId { get; set; }
        public string Status { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }

    public class BreachPenalty
    {
        public string BreachType { get; set; }
        public decimal PenaltyAmount { get; set; }
        public string PenaltyType { get; set; }
        public string Description { get; set; }
    }

    public class SLABreach
    {
        public int Id { get; set; }
        public int SLAId { get; set; }
        public string BreachDetails { get; set; }
        public DateTime BreachTime { get; set; }
        public DateTime RecordedAt { get; set; }
        public BreachStatus Status { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolutionDetails { get; set; }
    }

    public enum BreachStatus
    {
        Open,
        InProgress,
        Resolved,
        Escalated
    }

    public class SLAMetric
    {
        public string MetricName { get; set; }
        public double TargetValue { get; set; }
        public double ActualValue { get; set; }
        public double CompliancePercentage { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    // Knowledge Management Models
    public class KnowledgeArticle
    {
        public int Id { get; set; }
        public string ArticleNumber { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Summary { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string Category { get; set; }
        public ArticleStatus Status { get; set; }
        public string AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public int ViewCount { get; set; }
        public int Rating { get; set; }
        public int RatingCount { get; set; }
        public List<string> RelatedArticles { get; set; } = new List<string>();
        public bool IsPublic { get; set; }
        public List<KnowledgeFeedback> Feedback { get; set; } = new List<KnowledgeFeedback>();
    }

    public enum ArticleStatus
    {
        Draft,
        InReview,
        Published,
        Archived
    }

    public class KnowledgeFilter
    {
        public string Category { get; set; }
        public string Status { get; set; }
        public List<string> Tags { get; set; }
        public string Author { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class KnowledgeFeedback
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public string UserId { get; set; }
        public int Rating { get; set; }
        public string Comments { get; set; }
        public bool Helpful { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Self-Service Models
    public class SelfServiceRequest
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; }
        public string ServiceId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public SelfServiceStatus Status { get; set; }
        public string RequestedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? FulfilledAt { get; set; }
        public string FulfillmentDetails { get; set; }
        public Dictionary<string, object> RequestData { get; set; } = new Dictionary<string, object>();
        public List<SelfServiceApproval> Approvals { get; set; } = new List<SelfServiceApproval>();
    }

    public enum SelfServiceStatus
    {
        New,
        PendingApproval,
        Approved,
        InProgress,
        Fulfilled,
        Rejected,
        Cancelled
    }

    public class SelfServiceApproval
    {
        public string ApproverId { get; set; }
        public string Decision { get; set; }
        public string Comments { get; set; }
        public DateTime ApprovedAt { get; set; }
    }

    // ITIL Compliance Models
    public class ITILComplianceReport
    {
        public string ReportPeriod { get; set; }
        public DateTime GeneratedAt { get; set; }
        public double IncidentManagementCompliance { get; set; }
        public double ProblemManagementCompliance { get; set; }
        public double ChangeManagementCompliance { get; set; }
        public double AssetManagementCompliance { get; set; }
        public double OverallComplianceScore { get; set; }
        public List<ComplianceIssue> Issues { get; set; } = new List<ComplianceIssue>();
    }

    public class ComplianceIssue
    {
        public string Process { get; set; }
        public string Issue { get; set; }
        public string Severity { get; set; }
        public string Recommendation { get; set; }
    }

    public class ITILProcess
    {
        public string ProcessId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ITILRequirement> Requirements { get; set; } = new List<ITILRequirement>();
    }

    public class ITILRequirement
    {
        public string RequirementId { get; set; }
        public string Description { get; set; }
        public bool IsMandatory { get; set; }
        public string ValidationMethod { get; set; }
    }

    public class ITILAudit
    {
        public string ProcessId { get; set; }
        public DateTime AuditDate { get; set; }
        public string AuditorId { get; set; }
        public double ComplianceScore { get; set; }
        public List<AuditFinding> Findings { get; set; } = new List<AuditFinding>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    public class AuditFinding
    {
        public string Finding { get; set; }
        public string Severity { get; set; }
        public string Recommendation { get; set; }
        public string Status { get; set; }
    }

    #endregion
}
