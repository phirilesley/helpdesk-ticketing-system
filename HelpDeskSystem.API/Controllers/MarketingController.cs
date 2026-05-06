using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using HelpDeskSystem.Marketing.Services;
using HelpDeskSystem.API.DTOs.Marketing;

namespace HelpDeskSystem.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/marketing")]
    public class MarketingController : ControllerBase
    {
        private readonly IRealMarketingIntegrationService _marketingService;

        public MarketingController(IRealMarketingIntegrationService marketingService)
        {
            _marketingService = marketingService;
        }

        // HubSpot Integration
        [HttpPost("hubspot/contacts")]
        public async Task<IActionResult> CreateHubSpotContact([FromBody] CreateHubSpotContactDto request)
        {
            var contact = new HubSpotContact
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Company = request.Company,
                Title = request.Title,
                LifecycleStage = request.LifecycleStage,
                LeadStatus = request.LeadStatus
            };
            var result = await _marketingService.CreateHubSpotContact(contact);
            return Ok(result);
        }

        [HttpPut("hubspot/contacts/{contactId}")]
        public async Task<IActionResult> UpdateHubSpotContact(string contactId, [FromBody] UpdateHubSpotContactDto request)
        {
            var contact = new HubSpotContact
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Company = request.Company,
                Title = request.Title,
                LifecycleStage = request.LifecycleStage,
                LeadStatus = request.LeadStatus
            };
            var result = await _marketingService.UpdateHubSpotContact(contactId, contact);
            return Ok(result);
        }

        [HttpGet("hubspot/contacts")]
        public async Task<IActionResult> GetHubSpotContacts([FromQuery] HubSpotFilterDto request)
        {
            var filter = request != null ? new HubSpotFilter
            {
                LifecycleStage = request.LifecycleStage,
                LeadStatus = request.LeadStatus,
                CreatedAfter = request.CreatedAfter,
                CreatedBefore = request.CreatedBefore
            } : null;
            var contacts = await _marketingService.GetHubSpotContacts(filter);
            return Ok(contacts);
        }

        [HttpPost("hubspot/companies")]
        public async Task<IActionResult> CreateHubSpotCompany([FromBody] CreateHubSpotCompanyDto request)
        {
            var company = new HubSpotCompany
            {
                Name = request.Name,
                Domain = request.Domain,
                Phone = request.Phone,
                Address = request.Address,
                City = request.City,
                State = request.State,
                Zip = request.Zip,
                Country = request.Country,
                Description = request.Description,
                Industry = request.Industry,
                NumberOfEmployees = request.NumberOfEmployees,
                AnnualRevenue = request.AnnualRevenue
            };
            var result = await _marketingService.CreateHubSpotCompany(company);
            return Ok(result);
        }

        [HttpPost("hubspot/deals")]
        public async Task<IActionResult> CreateHubSpotDeal([FromBody] CreateHubSpotDealDto request)
        {
            var deal = new HubSpotDeal
            {
                Name = request.Name,
                Amount = request.Amount,
                Stage = request.Stage,
                CloseDate = request.CloseDate,
                Pipeline = request.Pipeline,
                Description = request.Description,
                Probability = request.Probability,
                ContactId = request.ContactId
            };
            var result = await _marketingService.CreateHubSpotDeal(deal);
            return Ok(result);
        }

        [HttpPost("hubspot/engagements")]
        public async Task<IActionResult> CreateHubSpotEngagement([FromBody] CreateHubSpotEngagementDto request)
        {
            var engagement = new HubSpotEngagement
            {
                ContactId = request.ContactId,
                EngagementType = request.EngagementType,
                Details = request.Details,
                Timestamp = request.Timestamp
            };
            var result = await _marketingService.CreateHubSpotEngagement(engagement);
            return Ok(result);
        }

        [HttpPost("hubspot/lists")]
        public async Task<IActionResult> CreateHubSpotList([FromBody] CreateHubSpotListDto request)
        {
            var list = new HubSpotList
            {
                Name = request.Name,
                Description = request.Description,
                ListType = request.ListType,
                Filters = request.Filters
            };
            var result = await _marketingService.CreateHubSpotList(list);
            return Ok(result);
        }

        [HttpPost("hubspot/email/send")]
        public async Task<IActionResult> SendHubSpotEmail([FromBody] SendHubSpotEmailDto request)
        {
            var email = new HubSpotEmail
            {
                From = request.From,
                To = request.To,
                Subject = request.Subject,
                HtmlContent = request.HtmlContent,
                TextContent = request.TextContent,
                CustomProperties = request.CustomProperties
            };
            var result = await _marketingService.SendHubSpotEmail(email);
            return Ok(result);
        }

        [HttpGet("hubspot/analytics")]
        public async Task<IActionResult> GetHubSpotAnalytics([FromQuery] HubSpotAnalyticsDto request)
        {
            var analytics = await _marketingService.GetHubSpotAnalytics(request.StartDate, request.EndDate);
            return Ok(analytics);
        }

        // Salesforce Integration
        [HttpPost("salesforce/contacts")]
        public async Task<IActionResult> CreateSalesforceContact([FromBody] CreateSalesforceContactDto request)
        {
            var contact = new SalesforceContact
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Title = request.Title,
                AccountId = request.AccountId,
                LeadSource = request.LeadSource,
                Description = request.Description
            };
            var result = await _marketingService.CreateSalesforceContact(contact);
            return Ok(result);
        }

        [HttpPut("salesforce/contacts/{contactId}")]
        public async Task<IActionResult> UpdateSalesforceContact(string contactId, [FromBody] UpdateSalesforceContactDto request)
        {
            var contact = new SalesforceContact
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Title = request.Title,
                AccountId = request.AccountId,
                LeadSource = request.LeadSource,
                Description = request.Description
            };
            var result = await _marketingService.UpdateSalesforceContact(contactId, contact);
            return Ok(result);
        }

        [HttpGet("salesforce/contacts")]
        public async Task<IActionResult> GetSalesforceContacts([FromQuery] SalesforceFilterDto request)
        {
            var filter = request != null ? new SalesforceFilter
            {
                LeadSource = request.LeadSource,
                AccountId = request.AccountId,
                CreatedAfter = request.CreatedAfter,
                CreatedBefore = request.CreatedBefore
            } : null;
            var contacts = await _marketingService.GetSalesforceContacts(filter);
            return Ok(contacts);
        }

        [HttpPost("salesforce/accounts")]
        public async Task<IActionResult> CreateSalesforceAccount([FromBody] CreateSalesforceAccountDto request)
        {
            var account = new SalesforceAccount
            {
                Name = request.Name,
                Type = request.Type,
                Industry = request.Industry,
                AnnualRevenue = request.AnnualRevenue,
                Phone = request.Phone,
                Website = request.Website,
                Description = request.Description,
                BillingCity = request.BillingCity,
                BillingState = request.BillingState,
                BillingPostalCode = request.BillingPostalCode,
                BillingCountry = request.BillingCountry,
                NumberOfEmployees = request.NumberOfEmployees
            };
            var result = await _marketingService.CreateSalesforceAccount(account);
            return Ok(result);
        }

        [HttpPost("salesforce/opportunities")]
        public async Task<IActionResult> CreateSalesforceOpportunity([FromBody] CreateSalesforceOpportunityDto request)
        {
            var opportunity = new SalesforceOpportunity
            {
                Name = request.Name,
                AccountId = request.AccountId,
                StageName = request.StageName,
                Amount = request.Amount,
                CloseDate = request.CloseDate,
                Description = request.Description,
                LeadSource = request.LeadSource,
                Probability = request.Probability,
                ForecastCategory = request.ForecastCategory
            };
            var result = await _marketingService.CreateSalesforceOpportunity(opportunity);
            return Ok(result);
        }

        [HttpPost("salesforce/leads")]
        public async Task<IActionResult> CreateSalesforceLead([FromBody] CreateSalesforceLeadDto request)
        {
            var lead = new SalesforceLead
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Company = request.Company,
                Status = request.Status,
                LeadSource = request.LeadSource,
                Description = request.Description
            };
            var result = await _marketingService.CreateSalesforceLead(lead);
            return Ok(result);
        }

        [HttpPost("salesforce/campaigns")]
        public async Task<IActionResult> CreateSalesforceCampaign([FromBody] CreateSalesforceCampaignDto request)
        {
            var campaign = new SalesforceCampaign
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                Status = request.Status,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                BudgetedCost = request.BudgetedCost,
                ExpectedRevenue = request.ExpectedRevenue
            };
            var result = await _marketingService.CreateSalesforceCampaign(campaign);
            return Ok(result);
        }

        [HttpPost("salesforce/reports/{reportId}")]
        public async Task<IActionResult> GenerateSalesforceReport(string reportId)
        {
            var report = await _marketingService.GenerateSalesforceReport(reportId);
            return Ok(report);
        }

        [HttpGet("salesforce/analytics")]
        public async Task<IActionResult> GetSalesforceAnalytics([FromQuery] SalesforceAnalyticsDto request)
        {
            var analytics = await _marketingService.GetSalesforceAnalytics(request.StartDate, request.EndDate);
            return Ok(analytics);
        }

        // Marketing Automation
        [HttpPost("automation/email-campaigns")]
        public async Task<IActionResult> CreateEmailCampaign([FromBody] CreateEmailCampaignDto request)
        {
            var campaign = new EmailCampaign
            {
                Name = request.Name,
                Subject = request.Subject,
                Content = request.Content,
                From = request.From,
                ReplyTo = request.ReplyTo,
                ListId = request.ListId,
                TemplateId = request.TemplateId,
                ScheduleDate = request.ScheduleDate,
                EnableABTest = request.EnableABTest,
                Personalization = request.Personalization
            };
            var result = await _marketingService.CreateEmailCampaign(campaign);
            return Ok(result);
        }

        [HttpPut("automation/email-campaigns/{campaignId}")]
        public async Task<IActionResult> UpdateEmailCampaign(string campaignId, [FromBody] UpdateEmailCampaignDto request)
        {
            var campaign = new EmailCampaign
            {
                Name = request.Name,
                Subject = request.Subject,
                Content = request.Content,
                From = request.From,
                ReplyTo = request.ReplyTo,
                ListId = request.ListId,
                TemplateId = request.TemplateId,
                ScheduleDate = request.ScheduleDate,
                EnableABTest = request.EnableABTest,
                Personalization = request.Personalization
            };
            var result = await _marketingService.UpdateEmailCampaign(campaignId, campaign);
            return Ok(result);
        }

        [HttpGet("automation/email-campaigns")]
        public async Task<IActionResult> GetEmailCampaigns([FromQuery] CampaignFilterDto request)
        {
            var filter = request != null ? new CampaignFilter
            {
                Status = request.Status,
                CreatedFrom = request.CreatedFrom,
                CreatedTo = request.CreatedTo,
                CampaignType = request.CampaignType
            } : null;
            var campaigns = await _marketingService.GetEmailCampaigns(filter);
            return Ok(campaigns);
        }

        [HttpGet("automation/email-campaigns/{campaignId}/metrics")]
        public async Task<IActionResult> GetCampaignMetrics(string campaignId)
        {
            var metrics = await _marketingService.GetCampaignMetrics(campaignId);
            return Ok(metrics);
        }

        [HttpPost("automation/email-templates")]
        public async Task<IActionResult> CreateEmailTemplate([FromBody] CreateEmailTemplateDto request)
        {
            var template = new EmailTemplate
            {
                Name = request.Name,
                Subject = request.Subject,
                HtmlContent = request.HtmlContent,
                TextContent = request.TextContent,
                Variables = request.Variables,
                Category = request.Category,
                IsActive = request.IsActive
            };
            var result = await _marketingService.CreateEmailTemplate(template);
            return Ok(result);
        }

        [HttpGet("automation/email-templates")]
        public async Task<IActionResult> GetEmailTemplates([FromQuery] TemplateFilterDto request)
        {
            var filter = request != null ? new TemplateFilter
            {
                Category = request.Category,
                IsActive = request.IsActive
            } : null;
            var templates = new List<EmailTemplate>();
            return Ok(templates);
        }

        [HttpPost("automation/lead-scoring/{contactId}")]
        public async Task<IActionResult> ScoreLead(string contactId)
        {
            var scoring = await _marketingService.ScoreLead(contactId);
            return Ok(scoring);
        }

        [HttpGet("automation/lead-scores")]
        public async Task<IActionResult> GetLeadScores([FromQuery] LeadFilterDto request)
        {
            var filter = request != null ? new LeadFilter
            {
                Status = request.Status,
                Grade = request.Grade,
                MinScore = request.MinScore,
                MaxScore = request.MaxScore
            } : null;
            var scores = await _marketingService.GetLeadScores(filter);
            return Ok(scores);
        }

        // Social Media Integration
        [HttpPost("social/posts")]
        public async Task<IActionResult> CreateSocialPost([FromBody] CreateSocialPostDto request)
        {
            var post = new SocialPost
            {
                Platform = request.Platform,
                Content = request.Content,
                Type = request.Type,
                MediaUrls = request.MediaUrls,
                Tags = request.Tags,
                ScheduledAt = request.ScheduledAt
            };
            var result = await _marketingService.CreateSocialPost(post);
            return Ok(result);
        }

        [HttpGet("social/posts")]
        public async Task<IActionResult> GetSocialPosts([FromQuery] SocialFilterDto request)
        {
            var filter = request != null ? new SocialFilter
            {
                Platform = request.Platform,
                Status = request.Status,
                PostedFrom = request.PostedFrom,
                PostedTo = request.PostedTo
            } : null;
            var posts = await _marketingService.GetSocialPosts(filter);
            return Ok(posts);
        }

        [HttpGet("social/posts/{postId}/engagement")]
        public async Task<IActionResult> GetSocialEngagement(string postId)
        {
            var engagement = await _marketingService.GetSocialEngagement(postId);
            return Ok(engagement);
        }

        [HttpGet("social/mentions")]
        public async Task<IActionResult> GetSocialMentions([FromQuery] SocialListeningDto request)
        {
            var mentions = await _marketingService.GetSocialMentions(request.BrandName, request.StartDate, request.EndDate);
            return Ok(mentions);
        }

        [HttpGet("social/analytics")]
        public async Task<IActionResult> GetSocialAnalytics([FromQuery] SocialAnalyticsDto request)
        {
            var analytics = await _marketingService.GetSocialAnalytics(request.Platform, request.StartDate, request.EndDate);
            return Ok(analytics);
        }

        [HttpPost("social/influencers")]
        public async Task<IActionResult> IdentifySocialInfluencers([FromBody] SocialInfluencerDto request)
        {
            var influencers = await _marketingService.IdentifySocialInfluencers(request.Topic, request.Location);
            return Ok(influencers);
        }

        // Content Management
        [HttpPost("content/pieces")]
        public async Task<IActionResult> CreateContent([FromBody] CreateContentDto request)
        {
            var content = new ContentPiece
            {
                Title = request.Title,
                Content = request.Content,
                Type = request.Type,
                Category = request.Category,
                Tags = request.Tags,
                AuthorId = request.AuthorId,
                PublishDate = request.PublishDate,
                SEO = request.SEO
            };
            var result = await _marketingService.CreateContent(content);
            return Ok(result);
        }

        [HttpGet("content/pieces")]
        public async Task<IActionResult> GetContent([FromQuery] ContentFilterDto request)
        {
            var filter = request != null ? new ContentFilter
            {
                Type = request.Type,
                Category = request.Category,
                Status = request.Status,
                AuthorId = request.AuthorId,
                PublishedFrom = request.PublishedFrom,
                PublishedTo = request.PublishedTo
            } : null;
            var content = await _marketingService.GetContent(filter);
            return Ok(content);
        }

        [HttpGet("content/calendar")]
        public async Task<IActionResult> GetContentCalendar([FromQuery] ContentCalendarDto request)
        {
            var calendar = await _marketingService.GetContentCalendar(request.StartDate, request.EndDate);
            return Ok(calendar);
        }

        [HttpGet("content/pieces/{contentId}/performance")]
        public async Task<IActionResult> GetContentPerformance(string contentId)
        {
            var performance = await _marketingService.GetContentPerformance(contentId);
            return Ok(performance);
        }

        [HttpPost("content/pieces/{contentId}/workflow")]
        public async Task<IActionResult> ManageContentWorkflow(string contentId, [FromBody] ContentWorkflowDto request)
        {
            var action = Enum.TryParse<WorkflowAction>(request.Action, true, out var parsed)
                ? parsed
                : WorkflowAction.Review;
            var result = await _marketingService.ManageContentWorkflow(contentId, action);
            return Ok(result);
        }

        [HttpPost("content/pieces/{contentId}/seo")]
        public async Task<IActionResult> OptimizeContentSEO(string contentId)
        {
            var seo = await _marketingService.OptimizeContentSEO(contentId);
            return Ok(seo);
        }

        // Analytics & Reporting
        [HttpGet("analytics")]
        public async Task<IActionResult> GetMarketingAnalytics([FromQuery] MarketingAnalyticsDto request)
        {
            var analytics = await _marketingService.GetMarketingAnalytics(request.StartDate, request.EndDate);
            return Ok(analytics);
        }

        [HttpGet("analytics/campaigns/{campaignId}/conversion")]
        public async Task<IActionResult> GetConversionMetrics(string campaignId)
        {
            var metrics = await _marketingService.GetConversionMetrics(campaignId);
            return Ok(metrics);
        }

        [HttpGet("analytics/attribution")]
        public async Task<IActionResult> GetAttributionModel()
        {
            var model = await _marketingService.GetAttributionModel();
            return Ok(model);
        }

        [HttpGet("analytics/customers/{customerId}/journey")]
        public async Task<IActionResult> GetCustomerJourneyAnalytics(string customerId)
        {
            var journey = await _marketingService.GetCustomerJourneyAnalytics(customerId);
            return Ok(journey);
        }

        [HttpGet("analytics/roi")]
        public async Task<IActionResult> GetROIAnalysis([FromQuery] ROIAnalysisDto request)
        {
            var analysis = await _marketingService.GetROIAnalysis(request.Period);
            return Ok(analysis);
        }

        [HttpGet("analytics/dashboard")]
        public async Task<IActionResult> GetMarketingDashboard()
        {
            var dashboard = await _marketingService.GetMarketingDashboard();
            return Ok(dashboard);
        }

        // Lead Management
        [HttpPost("leads")]
        public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto request)
        {
            var lead = new Lead
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Company = request.Company,
                Source = request.Source,
                Status = LeadStatus.New,
                Score = request.Score,
                AssignedTo = request.AssignedTo
            };
            var result = await _marketingService.CreateLead(lead);
            return Ok(result);
        }

        [HttpGet("leads")]
        public async Task<IActionResult> GetLeads([FromQuery] LeadFilterDto request)
        {
            var filter = request != null ? new LeadFilter
            {
                Status = request.Status,
                Source = request.Source,
                AssignedTo = request.AssignedTo,
                CreatedFrom = request.CreatedFrom,
                CreatedTo = request.CreatedTo,
                MinScore = request.MinScore,
                MaxScore = request.MaxScore
            } : null;
            var leads = await _marketingService.GetLeads(filter);
            return Ok(leads);
        }

        [HttpPost("leads/{leadId}/assign")]
        public async Task<IActionResult> AssignLead(string leadId, [FromBody] AssignLeadDto request)
        {
            var lead = await _marketingService.AssignLead(leadId, request.AssigneeId);
            return Ok(lead);
        }

        [HttpPut("leads/{leadId}/status")]
        public async Task<IActionResult> UpdateLeadStatus(string leadId, [FromBody] UpdateLeadStatusDto request)
        {
            var status = Enum.TryParse<LeadStatus>(request.Status, true, out var parsed)
                ? parsed
                : LeadStatus.New;
            var lead = await _marketingService.UpdateLeadStatus(leadId, status);
            return Ok(lead);
        }

        [HttpPost("leads/{leadId}/nurturing")]
        public async Task<IActionResult> StartLeadNurturing(string leadId, [FromBody] LeadNurturingDto request)
        {
            var campaign = new NurturingCampaign
            {
                CampaignId = request.CampaignId,
                Name = request.Name,
                Steps = request.Steps,
                Duration = request.Duration
            };
            var nurturing = await _marketingService.StartLeadNurturing(leadId, campaign);
            return Ok(nurturing);
        }

        [HttpPost("leads/{leadId}/convert")]
        public async Task<IActionResult> ConvertLeadToCustomer(string leadId, [FromBody] ConvertLeadDto request)
        {
            var conversion = new ConversionDetails
            {
                Type = request.Type,
                Description = request.Description,
                Value = request.Value,
                CampaignId = request.CampaignId
            };
            var result = await _marketingService.ConvertLeadToCustomer(leadId, conversion);
            return Ok(result);
        }

        // Customer Segmentation
        [HttpPost("segments")]
        public async Task<IActionResult> CreateCustomerSegment([FromBody] CreateCustomerSegmentDto request)
        {
            var segment = new CustomerSegment
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                Rules = request.Rules,
                IsActive = request.IsActive
            };
            var result = await _marketingService.CreateCustomerSegment(segment);
            return Ok(result);
        }

        [HttpGet("segments")]
        public async Task<IActionResult> GetCustomerSegments([FromQuery] SegmentFilterDto request)
        {
            var filter = request != null ? new SegmentFilter
            {
                Type = request.Type,
                IsActive = request.IsActive,
                Name = request.Name
            } : null;
            var segments = await _marketingService.GetCustomerSegments(filter);
            return Ok(segments);
        }

        [HttpGet("segments/{segmentId}/contacts")]
        public async Task<IActionResult> GetSegmentContacts(string segmentId)
        {
            var contacts = await _marketingService.GetSegmentContacts(segmentId);
            return Ok(contacts);
        }

        [HttpPost("segments/rules")]
        public async Task<IActionResult> CreateSegmentationRule([FromBody] SegmentationRuleDto request)
        {
            var rule = new SegmentationRule
            {
                Name = request.Name,
                Field = request.Field,
                Operator = request.Operator,
                Value = request.Value,
                Logic = request.Logic
            };
            var result = await _marketingService.CreateSegmentationRule(rule);
            return Ok(result);
        }

        [HttpPost("segments/update-membership")]
        public async Task<IActionResult> UpdateSegmentMembership()
        {
            var result = await _marketingService.UpdateSegmentMembership();
            return Ok(result);
        }

        // Marketing Automation Workflows
        [HttpPost("automation/workflows")]
        public async Task<IActionResult> CreateAutomationWorkflow([FromBody] CreateWorkflowDto request)
        {
            var mappedTriggerType = Enum.TryParse<TriggerType>(request.Trigger, true, out var triggerType)
                ? triggerType
                : TriggerType.Manual;
            var mappedSteps = request.Steps.Select((s, i) => new WorkflowStep
            {
                StepId = $"step-{i + 1}",
                Name = s.TryGetValue("name", out var n) ? n?.ToString() ?? $"Step {i + 1}" : $"Step {i + 1}",
                Type = StepType.Action,
                Configuration = System.Text.Json.JsonSerializer.Serialize(s),
                Order = i + 1
            }).ToList();
            var workflow = new AutomationWorkflow
            {
                Name = request.Name,
                Description = request.Description,
                Trigger = request.Trigger,
                Triggers = new List<WorkflowTrigger>
                {
                    new WorkflowTrigger
                    {
                        Name = request.Trigger,
                        Type = mappedTriggerType,
                        IsActive = request.IsActive
                    }
                },
                Steps = mappedSteps,
                Variables = request.Variables,
                IsActive = request.IsActive
            };
            var result = await _marketingService.CreateAutomationWorkflow(workflow);
            return Ok(result);
        }

        [HttpGet("automation/workflows")]
        public async Task<IActionResult> GetAutomationWorkflows([FromQuery] WorkflowFilterDto request)
        {
            var filter = request != null ? new WorkflowFilter
            {
                IsActive = request.IsActive,
                TriggerType = request.TriggerType
            } : null;
            var workflows = await _marketingService.GetAutomationWorkflows(filter);
            return Ok(workflows);
        }

        [HttpPost("automation/workflows/{workflowId}/execute")]
        public async Task<IActionResult> ExecuteWorkflow(string workflowId, [FromBody] ExecuteWorkflowDto request)
        {
            var triggerData = new Dictionary<string, object>(request.TriggerData);
            var execution = await _marketingService.ExecuteWorkflow(workflowId, triggerData);
            return Ok(execution);
        }

        [HttpGet("automation/workflows/executions")]
        public async Task<IActionResult> GetWorkflowExecutions([FromQuery] ExecutionFilterDto request)
        {
            var status = Enum.TryParse<ExecutionStatus>(request?.Status, true, out var parsedStatus)
                ? parsedStatus
                : (ExecutionStatus?)null;
            var filter = request != null ? new ExecutionFilter
            {
                WorkflowId = request.WorkflowId,
                Status = status,
                StartedFrom = request.StartedFrom,
                StartedTo = request.StartedTo
            } : null;
            var executions = await _marketingService.GetWorkflowExecutions(filter);
            return Ok(executions);
        }

        [HttpPost("automation/triggers")]
        public async Task<IActionResult> CreateWorkflowTrigger([FromBody] CreateTriggerDto request)
        {
            var type = Enum.TryParse<TriggerType>(request.Type, true, out var parsed)
                ? parsed
                : TriggerType.Manual;
            var trigger = new WorkflowTrigger
            {
                Name = request.Name,
                Type = type,
                Configuration = request.Configuration,
                IsActive = request.IsActive
            };
            var result = await _marketingService.CreateWorkflowTrigger(trigger);
            return Ok(result);
        }
    }
}
