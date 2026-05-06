using System;
using System.Collections.Generic;

namespace HelpDeskSystem.Marketing.Services
{
    public class MarketingSettings
    {
        public string? HubSpotAccessToken { get; set; }
        public string? SalesforceAccessToken { get; set; }
        public string? SalesforceUrl { get; set; }
    }

    public class HubSpotContact
    {
        public string? ContactId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Title { get; set; }
        public string? LifecycleStage { get; set; }
        public string? LeadStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? HubSpotId { get; set; }
    }

    public class HubSpotFilter
    {
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? LifecycleStage { get; set; }
        public string? LeadStatus { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
    }

    public class HubSpotContactResult
    {
        public string id { get; set; } = string.Empty;
        public HubSpotProperties properties { get; set; } = new();
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }

    public class HubSpotProperties
    {
        public string? firstname { get; set; }
        public string? lastname { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }
        public string? company { get; set; }
        public string? jobtitle { get; set; }
        public string? lifecyclestage { get; set; }
        public string? hs_lead_status { get; set; }
    }

    public class HubSpotResponse
    {
        public string id { get; set; } = string.Empty;
        public List<HubSpotContactResult> Results { get; set; } = new();
    }

    public class HubSpotCompany
    {
        public string? CompanyId { get; set; }
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
        public string? HubSpotId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class HubSpotDeal
    {
        public string? DealId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public DateTime? CloseDate { get; set; }
        public string? Pipeline { get; set; }
        public string? Description { get; set; }
        public double Probability { get; set; }
        public string ContactId { get; set; } = string.Empty;
        public string? HubSpotId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class HubSpotEngagement
    {
        public string? ContactId { get; set; }
        public string? EngagementType { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class HubSpotList
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ListType { get; set; }
        public List<Dictionary<string, string>> Filters { get; set; } = new();
    }

    public class HubSpotEmail
    {
        public string? EmailId { get; set; }
        public string? From { get; set; }
        public List<string> To { get; set; } = new();
        public string? Subject { get; set; }
        public string? HtmlContent { get; set; }
        public string? TextContent { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; } = new();
        public DateTime SentAt { get; set; }
        public string? Status { get; set; }
    }

    public class HubSpotEmailResponse
    {
        public string id { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public DateTime sentAt { get; set; }
    }

    public class HubSpotAnalytics { }

    public class SalesforceContact
    {
        public string? ContactId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Title { get; set; }
        public string? AccountId { get; set; }
        public string? LeadSource { get; set; }
        public string? Description { get; set; }
        public string? SalesforceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SalesforceFilter
    {
        public string? LeadSource { get; set; }
        public string? AccountId { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
    }

    public class SalesforceAccount
    {
        public string? AccountId { get; set; }
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
        public string? SalesforceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SalesforceOpportunity
    {
        public string? OpportunityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AccountId { get; set; }
        public string StageName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CloseDate { get; set; }
        public string? Description { get; set; }
        public string? LeadSource { get; set; }
        public double Probability { get; set; }
        public string? ForecastCategory { get; set; }
        public string? SalesforceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SalesforceLead
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Company { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? LeadSource { get; set; }
        public string? Description { get; set; }
    }

    public class SalesforceCampaign
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

    public class SalesforceReport { }
    public class SalesforceAnalytics { }

    public class EmailCampaign
    {
        public string? CampaignId { get; set; }
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
        public DateTime CreatedAt { get; set; }
        public CampaignStatus Status { get; set; }
        public int AudienceSize { get; set; }
    }

    public enum CampaignStatus { Draft, Scheduled, Sent, Canceled }

    public class CampaignFilter
    {
        public string? Status { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string? CampaignType { get; set; }
    }

    public class CampaignMetrics
    {
        public string CampaignId { get; set; } = string.Empty;
        public int Sent { get; set; }
        public decimal Cost { get; set; }
        public decimal ROI { get; set; }
        public int Impressions { get; set; }
        public int Clicks { get; set; }
        public int Conversions { get; set; }
        public int Delivered { get; set; }
        public int Opened { get; set; }
        public int Clicked { get; set; }
        public int Bounced { get; set; }
        public int Unsubscribed { get; set; }
        public double ConversionRate { get; set; }
        public decimal Revenue { get; set; }
    }

    public class EmailTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public List<string> Variables { get; set; } = new();
        public string? Category { get; set; }
        public bool IsActive { get; set; }
    }

    public class TemplateFilter
    {
        public string? Category { get; set; }
        public bool? IsActive { get; set; }
    }

    public class EmailAutomation { }

    public class LeadScoring
    {
        public string? ContactId { get; set; }
        public DateTime ScoredAt { get; set; }
        public double DemographicScore { get; set; }
        public double BehavioralScore { get; set; }
        public double EngagementScore { get; set; }
        public double PurchaseIntentScore { get; set; }
        public double OverallScore { get; set; }
        public string? LeadGrade { get; set; }
    }

    public class LeadFilter
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

    public class SocialPost
    {
        public string? PostId { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> MediaUrls { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public DateTime? ScheduledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public PostStatus Status { get; set; }
    }

    public enum PostStatus { Scheduled, Posted, Failed }

    public class SocialFilter
    {
        public string? Platform { get; set; }
        public string? Status { get; set; }
        public DateTime? PostedFrom { get; set; }
        public DateTime? PostedTo { get; set; }
    }

    public class SocialEngagement
    {
        public string PostId { get; set; } = string.Empty;
        public int Likes { get; set; }
        public int Comments { get; set; }
        public int Shares { get; set; }
        public int Clicks { get; set; }
        public int Impressions { get; set; }
        public int Reach { get; set; }
        public double EngagementRate { get; set; }
    }

    public class SocialListening
    {
        public string? BrandName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<SocialMention> Mentions { get; set; } = new();
        public int TotalMentions { get; set; }
        public int PositiveMentions { get; set; }
        public int NegativeMentions { get; set; }
        public int NeutralMentions { get; set; }
        public List<SocialMention> InfluencerMentions { get; set; } = new();
    }

    public class SocialMention
    {
        public string? Content { get; set; }
        public double Sentiment { get; set; }
        public bool IsInfluencer { get; set; }
    }

    public class SocialAnalytics { }
    public class SocialInfluencer { }

    public class ContentPiece
    {
        public string ContentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Category { get; set; }
        public List<string> Tags { get; set; } = new();
        public string AuthorId { get; set; } = string.Empty;
        public DateTime? PublishDate { get; set; }
        public Dictionary<string, string> SEO { get; set; } = new();
        public string Platform { get; set; } = string.Empty;
        public ContentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum ContentStatus { Draft, Review, Approved, Published, Archived }

    public class ContentFilter
    {
        public string? Type { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? AuthorId { get; set; }
        public DateTime? PublishedFrom { get; set; }
        public DateTime? PublishedTo { get; set; }
    }

    public class ContentCalendar
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ScheduledContent> ContentItems { get; set; } = new();
    }

    public class ScheduledContent
    {
        public string ContentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public DateTime PublishDate { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public ContentPerformance Performance { get; set; } = new();
    }
    public class ContentPerformance { }
    public class ContentWorkflow { }
    public enum WorkflowAction { Approve, Reject, Review, Publish }
    public class ContentSEO { }

    public class MarketingAnalytics
    {
        public string? Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        public object? CampaignMetrics { get; set; }
        public object? LeadMetrics { get; set; }
        public object? ConversionMetrics { get; set; }
        public object? SocialMetrics { get; set; }
        public object? ContentMetrics { get; set; }
        public object? ROIMetrics { get; set; }
    }

    public class ConversionMetrics { }
    public class AttributionModel { }
    public class CustomerJourney { }

    public class ROIAnalysis
    {
        public TimeSpan Period { get; set; }
        public DateTime GeneratedAt { get; set; }
        public decimal TotalInvestment { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal CustomerAcquisitionCost { get; set; }
        public decimal CustomerLifetimeValue { get; set; }
        public double ConversionRate { get; set; }
        public decimal ROI { get; set; }
        public object? BreakEvenPoint { get; set; }
    }

    public class ROIMetrics { }
    public class BreakEvenPoint { }
    public class MarketingDashboard { }

    public class Lead
    {
        public string? LeadId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Source { get; set; }
        public LeadStatus Status { get; set; }
        public double Score { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum LeadStatus { New, Contacted, Qualified, Lost, Converted }

    public class LeadNurturing
    {
        public string? LeadId { get; set; }
        public string? CampaignId { get; set; }
        public DateTime StartedAt { get; set; }
        public NurturingStatus Status { get; set; }
    }
    public class NurturingCampaign
    {
        public string? CampaignId { get; set; }
        public string? Name { get; set; }
        public List<string>? Steps { get; set; }
        public TimeSpan Duration { get; set; }
    }
    public class LeadConversion { }
    public class ConversionDetails
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public decimal Value { get; set; }
        public string? CampaignId { get; set; }
    }

    public class CustomerSegment
    {
        public string SegmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public List<Dictionary<string, string>> Rules { get; set; } = new();
        public bool IsActive { get; set; }
        public SegmentStatus Status { get; set; }
        public int Size { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum SegmentStatus { Active, Inactive, Archived }

    public class SegmentFilter
    {
        public string? Type { get; set; }
        public bool? IsActive { get; set; }
        public string? Name { get; set; }
    }

    // CRM Models
    public class CRMContact
    {
        public string ContactId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Title { get; set; }
        public ContactStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool CreateTicket { get; set; }
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    public enum ContactStatus { Active, Inactive, Lead, Customer }

    public class CRMFilter
    {
        public string? Status { get; set; }
        public string? Company { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
    }

    public class CRMCompany
    {
        public string CompanyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Industry { get; set; }
        public string? Size { get; set; }
        public string? Website { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CompanyFilter
    {
        public string? Industry { get; set; }
        public string? Size { get; set; }
        public string? Location { get; set; }
    }

    public class CRMDeal
    {
        public string DealId { get; set; } = string.Empty;
        public string ContactId { get; set; } = string.Empty;
        public string DealName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public DealStage Stage { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public DealStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public double DealScore { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public enum DealStage { Prospect, Qualified, Proposal, Negotiation, ClosedWon, ClosedLost }

    public enum DealStatus { Open, Won, Lost, Cancelled }

    public class DealFilter
    {
        public DealStage? Stage { get; set; }
        public DealStatus? Status { get; set; }
        public DateTime? CloseDateFrom { get; set; }
        public DateTime? CloseDateTo { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
    }

    public class CRMActivity
    {
        public string ActivityId { get; set; } = string.Empty;
        public string ContactId { get; set; } = string.Empty;
        public ActivityType Type { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ActivityDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum ActivityType { Call, Email, Meeting, Task, Note }

    public class ActivityFilter
    {
        public string? ContactId { get; set; }
        public ActivityType? Type { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    public enum LeadSource { Website, Email, Social, Referral, ColdCall, TradeShow }

    public enum NurturingStatus { Active, Paused, Completed, Converted }

    public enum SegmentType { Demographic, Behavioral, Geographic, Psychographic, Firmographic }

    public enum WorkflowStatus { Active, Inactive, Draft, Archived }

    public enum TriggerType { Event, Schedule, Webhook, Manual }

    public class WorkflowTrigger
    {
        public string TriggerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public TriggerType Type { get; set; }
        public object? Configuration { get; set; }
        public bool IsActive { get; set; }
    }

    public class WorkflowStep
    {
        public string StepId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public StepType Type { get; set; }
        public string? Configuration { get; set; }
        public int Order { get; set; }
    }

    public enum StepType { Action, Condition, Delay, Notification }

    public enum ExecutionStatus { Running, Completed, Failed, Cancelled }

    public class ExecutionStep
    {
        public string StepId { get; set; } = string.Empty;
        public ExecutionStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Result { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }



    public class CustomerJourneyAnalytics { }

    public class ContentApproval
    {
        public string ApprovalId { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public string ReviewerId { get; set; } = string.Empty;
        public ApprovalDecision Decision { get; set; }
        public string? Comments { get; set; }
        public DateTime ReviewedAt { get; set; }
    }

    public enum ApprovalDecision { Approved, Rejected, RequestedChanges }

    public class SegmentationRule
    {
        public string RuleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Logic { get; set; } = string.Empty;
    }

    public class AutomationWorkflow
    {
        public string WorkflowId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public WorkflowStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<WorkflowTrigger> Triggers { get; set; } = new();
        public string? Trigger { get; set; }
        public List<WorkflowStep> Steps { get; set; } = new();
        public List<string> Variables { get; set; } = new();
        public bool IsActive { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public class WorkflowFilter
    {
        public WorkflowStatus? Status { get; set; }
        public bool? IsActive { get; set; }
        public string? TriggerType { get; set; }
    }

    public class WorkflowExecution
    {
        public string ExecutionId { get; set; } = string.Empty;
        public string WorkflowId { get; set; } = string.Empty;
        public Dictionary<string, object> TriggerData { get; set; } = new();
        public ExecutionStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<ExecutionStep> Steps { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class ExecutionFilter
    {
        public string? WorkflowId { get; set; }
        public ExecutionStatus? Status { get; set; }
        public DateTime? StartedFrom { get; set; }
        public DateTime? StartedTo { get; set; }
    }



    public class SalesforceResponse
    {
        public string id { get; set; } = string.Empty;
        public bool success { get; set; }
        public List<SalesforceError> errors { get; set; } = new();
    }

    public class SalesforceError
    {
        public string statusCode { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string fields { get; set; } = string.Empty;
    }

    public class CampaignAnalytics { }
    public class LeadAnalytics { }
    public class ConversionAnalytics { }
    public class ContentAnalytics { }
}
