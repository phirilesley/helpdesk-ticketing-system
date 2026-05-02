using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Shared.Base;

namespace HelpDeskSystem.Domain.Entities;

public class IdentityProviderConfig : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public IdentityProtocol Protocol { get; set; } = IdentityProtocol.Oidc;
    public string Issuer { get; set; } = string.Empty;
    public string AuthorityOrMetadataUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool EnforceSso { get; set; }
    public bool EnforceStrictIssuer { get; set; } = true;
    public string AllowedRedirectUrisJson { get; set; } = "[]";
    public bool OidcRequirePkce { get; set; } = true;
    public bool SamlValidateSignature { get; set; } = true;
    public bool SamlAllowIdpInitiated { get; set; }
    public string SamlMetadataXml { get; set; } = string.Empty;
    public string SamlAllowedCertificateThumbprints { get; set; } = string.Empty;
    public string SamlSpEntityId { get; set; } = string.Empty;
    public string SamlAcsUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class AbacPolicyRule : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ConditionJson { get; set; } = string.Empty;
    public PolicyEffect Effect { get; set; } = PolicyEffect.Allow;
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
}

public class OmnichannelConnector : BaseEntity
{
    public int TenantId { get; set; }
    public ChannelType ChannelType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = string.Empty;
    public string InboundSigningSecretHash { get; set; } = string.Empty;
    public string SignatureHeaderName { get; set; } = "X-Channel-Signature";
    public string SignatureAlgorithm { get; set; } = "hmac-sha256";
    public int DedupWindowMinutes { get; set; } = 120;
    public ConnectorStatus Status { get; set; } = ConnectorStatus.Disabled;
}

public class InboundChannelEvent : BaseEntity
{
    public int TenantId { get; set; }
    public int ConnectorId { get; set; }
    public ChannelType ChannelType { get; set; }
    public string ExternalConversationId { get; set; } = string.Empty;
    public string ExternalMessageId { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string RawPayloadJson { get; set; } = string.Empty;
    public string NormalizedPayloadJson { get; set; } = string.Empty;
    public DateTime? ExternalTimestampUtc { get; set; }
    public InboundEventStatus Status { get; set; } = InboundEventStatus.Received;
    public int? CreatedTicketId { get; set; }
    public string ProcessingError { get; set; } = string.Empty;
}

public class WorkflowDefinition : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsPublished { get; set; }
    public string GraphJson { get; set; } = string.Empty;
    public string GuardrailJson { get; set; } = "{}";
    public int MaxLoopCount { get; set; } = 3;
}

public class LegalHoldCase : BaseEntity
{
    public int TenantId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ScopeJson { get; set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DataSubjectRequest : BaseEntity
{
    public int TenantId { get; set; }
    public DataSubjectRequestType RequestType { get; set; }
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.Open;
    public DsrProcessStage Stage { get; set; } = DsrProcessStage.Intake;
    public string SubjectEmail { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? CompletedAtUtc { get; set; }
}

public class IntegrationApp : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class MarketplaceApp : BaseEntity
{
    public string AppKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ManifestJson { get; set; } = string.Empty;
    public string MinPlanName { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class TenantAppInstall : BaseEntity
{
    public int TenantId { get; set; }
    public int MarketplaceAppId { get; set; }
    public AppInstallStatus Status { get; set; } = AppInstallStatus.Pending;
    public string InstalledVersion { get; set; } = "1.0.0";
    public string ConfigJson { get; set; } = "{}";
    public string LastError { get; set; } = string.Empty;
}

public class WebhookSubscription : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public string EventFiltersJson { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int MaxAttempts { get; set; } = 5;
    public int RetryBackoffSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 20;
    public DateTime? LastDeliveryAtUtc { get; set; }
}

public class ServiceProject : BaseEntity
{
    public int TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WorkflowConfigJson { get; set; } = string.Empty;
}

public class IssueDependency : BaseEntity
{
    public int TenantId { get; set; }
    public int SourceTicketId { get; set; }
    public int DependsOnTicketId { get; set; }
    public string DependencyType { get; set; } = "blocks";
}

public class ReleasePlan : BaseEntity
{
    public int TenantId { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime TargetDateUtc { get; set; }
    public string ScopeTicketIdsJson { get; set; } = "[]";
    public string DependencyGraphJson { get; set; } = "{}";
}

public class SprintMetric : BaseEntity
{
    public int TenantId { get; set; }
    public int ProjectId { get; set; }
    public string SprintName { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int PlannedStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public int PlannedIssues { get; set; }
    public int CompletedIssues { get; set; }
    public decimal Velocity { get; set; }
    public decimal Burnup { get; set; }
    public decimal Burndown { get; set; }
    public decimal CycleTimeHours { get; set; }
}

public class BillingPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPriceUsd { get; set; }
    public int IncludedAgentSeats { get; set; }
    public int IncludedTicketsPerMonth { get; set; }
    public string EntitlementsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}

public class TenantSubscription : BaseEntity
{
    public int TenantId { get; set; }
    public int BillingPlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;
    public DateTime CurrentPeriodStartUtc { get; set; }
    public DateTime CurrentPeriodEndUtc { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string EntitlementOverridesJson { get; set; } = "{}";
}

public class Invoice : BaseEntity
{
    public int TenantId { get; set; }
    public int? TenantSubscriptionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public decimal SubtotalUsd { get; set; }
    public decimal TaxUsd { get; set; }
    public decimal TotalUsd { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string LineItemsJson { get; set; } = "[]";
}

public class SlaPauseRule : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConditionJson { get; set; } = "{}";
    public bool PauseResponseSla { get; set; } = true;
    public bool PauseResolutionSla { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
}

public class SlaBreachAction : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BreachType { get; set; } = "resolution";
    public int TriggerAfterBreachMinutes { get; set; }
    public int ExecutionOrder { get; set; } = 10;
    public string ActionType { get; set; } = "notify_role";
    public string ActionConfigJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
}

public class DsrProcessingLog : BaseEntity
{
    public int TenantId { get; set; }
    public int DataSubjectRequestId { get; set; }
    public DsrProcessStage Stage { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int? PerformedByUserId { get; set; }
}

public class UsageMeter : BaseEntity
{
    public int TenantId { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public DateTime UsageDateUtc { get; set; }
    public decimal Quantity { get; set; }
}
