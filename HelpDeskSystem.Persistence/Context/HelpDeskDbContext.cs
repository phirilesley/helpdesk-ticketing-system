using HelpDeskSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
