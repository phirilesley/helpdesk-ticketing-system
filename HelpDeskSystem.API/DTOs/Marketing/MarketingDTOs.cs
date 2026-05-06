using System;
using System.Collections.Generic;

namespace HelpDeskSystem.API.DTOs.Marketing
{
    public class CreateHubSpotContactDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Title { get; set; }
        public string? LifecycleStage { get; set; }
        public string? LeadStatus { get; set; }
    }

    public class UpdateHubSpotContactDto : CreateHubSpotContactDto { }

    public class HubSpotFilterDto
    {
        public string? LifecycleStage { get; set; }
        public string? LeadStatus { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
    }

    public class CreateHubSpotCompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Country { get; set; }
        public string? Description { get; set; }
        public string? Industry { get; set; }
        public int? NumberOfEmployees { get; set; }
        public decimal? AnnualRevenue { get; set; }
    }

    public class CreateHubSpotDealDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public DateTime? CloseDate { get; set; }
        public string? Pipeline { get; set; }
        public string? Description { get; set; }
        public double Probability { get; set; }
        public string ContactId { get; set; } = string.Empty;
    }

    public class CreateHubSpotEngagementDto
    {
        public string ContactId { get; set; } = string.Empty;
        public string EngagementType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class CreateHubSpotListDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ListType { get; set; } = "STATIC";
        public List<Dictionary<string, string>> Filters { get; set; } = new();
    }

    public class SendHubSpotEmailDto
    {
        public string From { get; set; } = string.Empty;
        public List<string> To { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; } = new();
    }

    public class HubSpotAnalyticsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CreateSalesforceContactDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Title { get; set; }
        public string? AccountId { get; set; }
        public string? LeadSource { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateSalesforceContactDto : CreateSalesforceContactDto { }

    public class SalesforceFilterDto
    {
        public string? LeadSource { get; set; }
        public string? AccountId { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
    }

    public class CreateSalesforceAccountDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Industry { get; set; }
        public decimal? AnnualRevenue { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Description { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingState { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? BillingCountry { get; set; }
        public int? NumberOfEmployees { get; set; }
    }

    public class CreateSalesforceOpportunityDto
    {
        public string Name { get; set; } = string.Empty;
        public string? AccountId { get; set; }
        public string StageName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CloseDate { get; set; }
        public string? Description { get; set; }
        public string? LeadSource { get; set; }
        public double Probability { get; set; }
        public string? ForecastCategory { get; set; }
    }

    public class CreateSalesforceLeadDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Company { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public string? LeadSource { get; set; }
        public string? Description { get; set; }
    }

    public class CreateSalesforceCampaignDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? BudgetedCost { get; set; }
        public decimal? ExpectedRevenue { get; set; }
    }

    public class SalesforceAnalyticsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CreateEmailCampaignDto
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string? ReplyTo { get; set; }
        public string ListId { get; set; } = string.Empty;
        public string? TemplateId { get; set; }
        public DateTime? ScheduleDate { get; set; }
        public bool EnableABTest { get; set; }
        public Dictionary<string, string> Personalization { get; set; } = new();
    }

    public class UpdateEmailCampaignDto : CreateEmailCampaignDto { }

    public class CampaignFilterDto
    {
        public string? Status { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string? CampaignType { get; set; }
    }

    public class CreateEmailTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public List<string> Variables { get; set; } = new();
        public string? Category { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class TemplateFilterDto
    {
        public string? Category { get; set; }
        public bool? IsActive { get; set; }
    }

    public class LeadFilterDto
    {
        public string? Status { get; set; }
        public string? Grade { get; set; }
        public string? Source { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public double? MinScore { get; set; }
        public double? MaxScore { get; set; }
    }

    public class CreateSocialPostDto
    {
        public string Platform { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "TEXT";
        public List<string> MediaUrls { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public DateTime? ScheduledAt { get; set; }
    }

    public class SocialFilterDto
    {
        public string? Platform { get; set; }
        public string? Status { get; set; }
        public DateTime? PostedFrom { get; set; }
        public DateTime? PostedTo { get; set; }
    }

    public class SocialListeningDto
    {
        public string BrandName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SocialAnalyticsDto
    {
        public string? Platform { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SocialInfluencerDto
    {
        public string Topic { get; set; } = string.Empty;
        public string? Location { get; set; }
    }

    public class CreateContentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "BLOG";
        public string? Category { get; set; }
        public List<string> Tags { get; set; } = new();
        public string AuthorId { get; set; } = string.Empty;
        public DateTime? PublishDate { get; set; }
        public Dictionary<string, string> SEO { get; set; } = new();
    }

    public class ContentFilterDto
    {
        public string? Type { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? AuthorId { get; set; }
        public DateTime? PublishedFrom { get; set; }
        public DateTime? PublishedTo { get; set; }
    }

    public class ContentCalendarDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ContentWorkflowDto
    {
        public string Action { get; set; } = string.Empty;
    }

    public class MarketingAnalyticsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ROIAnalysisDto
    {
        public TimeSpan Period { get; set; }
    }

    public class CreateLeadDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Source { get; set; }
        public double Score { get; set; }
        public string? AssignedTo { get; set; }
    }

    public class AssignLeadDto
    {
        public string AssigneeId { get; set; } = string.Empty;
    }

    public class UpdateLeadStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class LeadNurturingDto
    {
        public string CampaignId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    public class ConvertLeadDto
    {
        public string Type { get; set; } = "CUSTOMER";
        public string? Description { get; set; }
        public decimal Value { get; set; }
        public string? CampaignId { get; set; }
    }

    public class CreateCustomerSegmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "DYNAMIC";
        public List<Dictionary<string, string>> Rules { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class SegmentFilterDto
    {
        public string? Type { get; set; }
        public bool? IsActive { get; set; }
        public string? Name { get; set; }
    }

    public class SegmentationRuleDto
    {
        public string Name { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = "EQUALS";
        public string Value { get; set; } = string.Empty;
        public string Logic { get; set; } = "AND";
    }

    public class CreateWorkflowDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Trigger { get; set; } = string.Empty;
        public List<Dictionary<string, object>> Steps { get; set; } = new();
        public List<string> Variables { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class WorkflowFilterDto
    {
        public bool? IsActive { get; set; }
        public string? TriggerType { get; set; }
    }

    public class ExecuteWorkflowDto
    {
        public Dictionary<string, object> TriggerData { get; set; } = new();
    }

    public class ExecutionFilterDto
    {
        public string? WorkflowId { get; set; }
        public string? Status { get; set; }
        public DateTime? StartedFrom { get; set; }
        public DateTime? StartedTo { get; set; }
    }

    public class CreateTriggerDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, string> Configuration { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }
}
