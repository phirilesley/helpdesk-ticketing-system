using HelpDeskSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HelpDeskSystem.Persistence.Context;

public class HelpDeskDbContext : DbContext
{
    public HelpDeskDbContext(DbContextOptions<HelpDeskDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketMessage> TicketMessages { get; set; }
    public DbSet<TicketAssignment> TicketAssignments { get; set; }
    public DbSet<TicketAttachment> TicketAttachments { get; set; }
    public DbSet<TicketStatusHistory> TicketStatusHistories { get; set; }
    public DbSet<TicketPriority> TicketPriorities { get; set; }
    public DbSet<TicketCategory> TicketCategories { get; set; }
    public DbSet<TicketSlaRule> TicketSlaRules { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AutomationRule> AutomationRules { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<KnowledgeBaseCategory> KnowledgeBaseCategories { get; set; }
    public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles { get; set; }
    public DbSet<KnowledgeBaseArticleVersion> KnowledgeBaseArticleVersions { get; set; }
    public DbSet<KnowledgeBaseArticleFeedback> KnowledgeBaseArticleFeedback { get; set; }
    public DbSet<TenantPortalSetting> TenantPortalSettings { get; set; }
    public DbSet<InboundEmailLog> InboundEmailLogs { get; set; }
    public DbSet<BusinessHoursProfile> BusinessHoursProfiles { get; set; }
    public DbSet<TenantSecurityPolicy> TenantSecurityPolicies { get; set; }
    public DbSet<EscalationRule> EscalationRules { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<SlaBreachLog> SlaBreachLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // New entities for enhanced features
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailNotification> EmailNotifications { get; set; }
    public DbSet<WorkflowRule> WorkflowRules { get; set; }
    public DbSet<WorkflowExecution> WorkflowExecutions { get; set; }
    public DbSet<IdentityProviderConfig> IdentityProviderConfigs { get; set; }
    public DbSet<AbacPolicyRule> AbacPolicyRules { get; set; }
    public DbSet<OmnichannelConnector> OmnichannelConnectors { get; set; }
    public DbSet<InboundChannelEvent> InboundChannelEvents { get; set; }
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
    public DbSet<LegalHoldCase> LegalHoldCases { get; set; }
    public DbSet<DataSubjectRequest> DataSubjectRequests { get; set; }
    public DbSet<IntegrationApp> IntegrationApps { get; set; }
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
    public DbSet<MarketplaceApp> MarketplaceApps { get; set; }
    public DbSet<TenantAppInstall> TenantAppInstalls { get; set; }
    public DbSet<ServiceProject> ServiceProjects { get; set; }
    public DbSet<IssueDependency> IssueDependencies { get; set; }
    public DbSet<ReleasePlan> ReleasePlans { get; set; }
    public DbSet<SprintMetric> SprintMetrics { get; set; }
    public DbSet<BillingPlan> BillingPlans { get; set; }
    public DbSet<TenantSubscription> TenantSubscriptions { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<UsageMeter> UsageMeters { get; set; }
    public DbSet<SlaPauseRule> SlaPauseRules { get; set; }
    public DbSet<SlaBreachAction> SlaBreachActions { get; set; }
    public DbSet<DsrProcessingLog> DsrProcessingLogs { get; set; }
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.ExpiresAtUtc });

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.FamilyId });

        modelBuilder.Entity<AutomationRule>()
            .HasIndex(ar => new { ar.IsActive, ar.TriggerType, ar.TenantId });

        modelBuilder.Entity<IdentityProviderConfig>()
            .HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique();

        modelBuilder.Entity<AbacPolicyRule>()
            .HasIndex(x => new { x.TenantId, x.Resource, x.Action, x.Priority });

        modelBuilder.Entity<OmnichannelConnector>()
            .HasIndex(x => new { x.TenantId, x.ChannelType, x.Name })
            .IsUnique();

        modelBuilder.Entity<InboundChannelEvent>()
            .HasIndex(x => new { x.ConnectorId, x.ExternalMessageId });

        modelBuilder.Entity<WorkflowDefinition>()
            .HasIndex(x => new { x.TenantId, x.Name, x.Version })
            .IsUnique();

        modelBuilder.Entity<LegalHoldCase>()
            .HasIndex(x => new { x.TenantId, x.CaseNumber })
            .IsUnique();

        modelBuilder.Entity<DataSubjectRequest>()
            .HasIndex(x => new { x.TenantId, x.ReferenceNumber })
            .IsUnique();

        modelBuilder.Entity<IntegrationApp>()
            .HasIndex(x => new { x.TenantId, x.Provider, x.Name });

        modelBuilder.Entity<WebhookSubscription>()
            .HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique();

        modelBuilder.Entity<MarketplaceApp>()
            .HasIndex(x => x.AppKey)
            .IsUnique();

        modelBuilder.Entity<TenantAppInstall>()
            .HasIndex(x => new { x.TenantId, x.MarketplaceAppId })
            .IsUnique();

        modelBuilder.Entity<ServiceProject>()
            .HasIndex(x => new { x.TenantId, x.Key })
            .IsUnique();

        modelBuilder.Entity<IssueDependency>()
            .HasIndex(x => new { x.TenantId, x.SourceTicketId, x.DependsOnTicketId })
            .IsUnique();

        modelBuilder.Entity<ReleasePlan>()
            .HasIndex(x => new { x.TenantId, x.ProjectId, x.Name });

        modelBuilder.Entity<SprintMetric>()
            .HasIndex(x => new { x.TenantId, x.ProjectId, x.SprintName })
            .IsUnique();

        modelBuilder.Entity<TenantSubscription>()
            .HasIndex(x => x.TenantId);

        modelBuilder.Entity<Invoice>()
            .HasIndex(x => new { x.TenantId, x.InvoiceNumber })
            .IsUnique();

        modelBuilder.Entity<UsageMeter>()
            .HasIndex(x => new { x.TenantId, x.MetricName, x.UsageDateUtc });

        modelBuilder.Entity<SlaPauseRule>()
            .HasIndex(x => new { x.TenantId, x.IsEnabled });

        modelBuilder.Entity<SlaBreachAction>()
            .HasIndex(x => new { x.TenantId, x.BreachType, x.ExecutionOrder });

        modelBuilder.Entity<DsrProcessingLog>()
            .HasIndex(x => new { x.TenantId, x.DataSubjectRequestId, x.CreatedAtUtc });

        modelBuilder.Entity<WebhookDelivery>()
            .HasIndex(d => new { d.SubscriptionId, d.Status, d.NextAttemptAtUtc });

        modelBuilder.Entity<WebhookDelivery>()
            .HasOne(d => d.Subscription)
            .WithMany()
            .HasForeignKey(d => d.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkflowExecution>()
            .HasOne(x => x.WorkflowRule)
            .WithMany(x => x.Executions)
            .HasForeignKey(x => x.WorkflowRuleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WorkflowExecution>()
            .HasOne(x => x.Ticket)
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<KnowledgeBaseCategory>()
            .HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique();

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .HasIndex(x => new { x.TenantId, x.Slug })
            .IsUnique();

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .HasIndex(x => new { x.TenantId, x.IsPublished, x.CreatedAtUtc });

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .HasOne(x => x.Tenant)
            .WithMany(x => x.KnowledgeBaseArticles)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<KnowledgeBaseArticleFeedback>()
            .HasIndex(x => new { x.ArticleId, x.UserId });

        modelBuilder.Entity<TenantPortalSetting>()
            .HasIndex(x => x.TenantId)
            .IsUnique();

        modelBuilder.Entity<TenantSecurityPolicy>()
            .HasIndex(x => x.TenantId)
            .IsUnique();

        modelBuilder.Entity<TenantSecurityPolicy>()
            .Property(x => x.AllowedIpRanges)
            .HasMaxLength(2000);

        modelBuilder.Entity<InboundEmailLog>()
            .HasIndex(x => x.ExternalMessageId)
            .IsUnique();

        modelBuilder.Entity<BusinessHoursProfile>()
            .HasIndex(x => new { x.TenantId, x.IsDefault });

        modelBuilder.Entity<BusinessHoursProfile>()
            .Property(x => x.TimeZoneId)
            .HasMaxLength(128);

        modelBuilder.Entity<BusinessHoursProfile>()
            .Property(x => x.WorkingDays)
            .HasMaxLength(32);

        modelBuilder.Entity<KnowledgeBaseCategory>()
            .Property(x => x.Name)
            .HasMaxLength(120);

        modelBuilder.Entity<EmailTemplate>()
            .Property(x => x.DefaultVariables)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, string>>(
                (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
                value => JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new Dictionary<string, string>()));

        modelBuilder.Entity<BillingPlan>()
            .Property(x => x.MonthlyPriceUsd)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Invoice>()
            .Property(x => x.SubtotalUsd)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Invoice>()
            .Property(x => x.TaxUsd)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Invoice>()
            .Property(x => x.TotalUsd)
            .HasPrecision(18, 2);

        modelBuilder.Entity<UsageMeter>()
            .Property(x => x.Quantity)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SprintMetric>()
            .Property(x => x.Velocity)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SprintMetric>()
            .Property(x => x.Burnup)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SprintMetric>()
            .Property(x => x.Burndown)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SprintMetric>()
            .Property(x => x.CycleTimeHours)
            .HasPrecision(18, 2);

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .Property(x => x.Slug)
            .HasMaxLength(180);

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .Property(x => x.Title)
            .HasMaxLength(200);

        modelBuilder.Entity<InboundEmailLog>()
            .Property(x => x.ExternalMessageId)
            .HasMaxLength(255);

        modelBuilder.Entity<InboundEmailLog>()
            .Property(x => x.FromEmail)
            .HasMaxLength(255);

        modelBuilder.Entity<AuditLog>()
            .Property(al => al.Action)
            .HasMaxLength(128);

        modelBuilder.Entity<AuditLog>()
            .Property(al => al.EntityName)
            .HasMaxLength(128);

        modelBuilder.Entity<AuditLog>()
            .Property(al => al.EntityId)
            .HasMaxLength(200);

        modelBuilder.Entity<AuditLog>()
            .Property(al => al.IpAddress)
            .HasMaxLength(64);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => al.CreatedAtUtc);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => new { al.Action, al.CreatedAtUtc });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => new { al.UserId, al.CreatedAtUtc });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => new { al.EntityName, al.EntityId, al.CreatedAtUtc });
    }
}
