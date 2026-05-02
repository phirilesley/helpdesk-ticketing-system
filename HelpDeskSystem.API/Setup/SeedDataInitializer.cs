using HelpDeskSystem.Application.DTOs.Users;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Setup;

public static class SeedDataInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var options = configuration.GetSection(SeedDataOptions.SectionName).Get<SeedDataOptions>() ?? new SeedDataOptions();
        if (!options.Enabled)
            return;

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        await context.Database.MigrateAsync(cancellationToken);

        var tenant = await EnsureTenantAsync(context, options, cancellationToken);
        await EnsureRolesAsync(context, cancellationToken);
        var adminUser = await EnsureSuperAdminUserAsync(userService, options, tenant.Id);
        await EnsureRoleAssignedAsync(context, adminUser.Id, "SuperAdmin", cancellationToken);
        await EnsureRoleAssignedAsync(context, adminUser.Id, "Admin", cancellationToken);
        await EnsureTicketMetadataAsync(context, cancellationToken);
        await EnsureEscalationRulesAsync(context, cancellationToken);
        await EnsureAutomationRulesAsync(context, cancellationToken);
        await EnsurePortalDefaultsAsync(context, tenant.Id, cancellationToken);
        await EnsureTenantSecurityPolicyAsync(context, tenant.Id, cancellationToken);
        await EnsureEnterpriseDefaultsAsync(context, tenant.Id, cancellationToken);
    }

    private static async Task<Tenant> EnsureTenantAsync(HelpDeskDbContext context, SeedDataOptions options, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Domain == options.DefaultTenantDomain, cancellationToken);

        if (tenant != null)
            return tenant;

        tenant = new Tenant
        {
            Name = options.DefaultTenantName,
            Domain = options.DefaultTenantDomain,
            IsActive = true
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    private static async Task EnsureRolesAsync(HelpDeskDbContext context, CancellationToken cancellationToken)
    {
        var roleNames = new[] { "SuperAdmin", "Admin", "Agent", "Customer" };

        foreach (var roleName in roleNames)
        {
            if (!await context.Roles.AnyAsync(r => r.Name == roleName, cancellationToken))
            {
                context.Roles.Add(new Role
                {
                    Name = roleName,
                    Description = $"{roleName} role",
                    TenantId = null
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<UserDto> EnsureSuperAdminUserAsync(
        IUserService userService,
        SeedDataOptions options,
        int tenantId)
    {
        var existing = await userService.GetUserByEmailAsync(options.SuperAdminEmail);
        if (existing != null)
            return existing;

        var created = await userService.CreateUserAsync(new CreateUserDto
        {
            Username = options.SuperAdminUsername,
            Email = options.SuperAdminEmail,
            Password = options.SuperAdminPassword,
            FirstName = options.SuperAdminFirstName,
            LastName = options.SuperAdminLastName,
            TenantId = tenantId
        });

        return created;
    }

    private static async Task EnsureRoleAssignedAsync(
        HelpDeskDbContext context,
        int userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role == null)
            return;

        if (await context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, cancellationToken))
            return;

        context.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = role.Id
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureTicketMetadataAsync(HelpDeskDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.TicketCategories.AnyAsync(cancellationToken))
        {
            context.TicketCategories.AddRange(
                new TicketCategory { Name = "Technical", Description = "Technical support requests" },
                new TicketCategory { Name = "Billing", Description = "Billing and payments" },
                new TicketCategory { Name = "General", Description = "General inquiries" });
        }

        if (!await context.TicketPriorities.AnyAsync(cancellationToken))
        {
            context.TicketPriorities.AddRange(
                new TicketPriority { Name = "Low", Level = 1, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 2880 },
                new TicketPriority { Name = "Medium", Level = 2, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 1440 },
                new TicketPriority { Name = "High", Level = 3, ResponseTimeMinutes = 60, ResolutionTimeMinutes = 480 },
                new TicketPriority { Name = "Critical", Level = 4, ResponseTimeMinutes = 15, ResolutionTimeMinutes = 120 });
        }

        await context.SaveChangesAsync(cancellationToken);

        var categories = await context.TicketCategories.AsNoTracking().ToListAsync(cancellationToken);
        var priorities = await context.TicketPriorities.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var category in categories)
        {
            foreach (var priority in priorities)
            {
                var exists = await context.TicketSlaRules.AnyAsync(
                    r => r.CategoryId == category.Id && r.PriorityId == priority.Id,
                    cancellationToken);

                if (exists)
                    continue;

                context.TicketSlaRules.Add(new TicketSlaRule
                {
                    CategoryId = category.Id,
                    PriorityId = priority.Id,
                    ResponseTimeMinutes = priority.ResponseTimeMinutes,
                    ResolutionTimeMinutes = priority.ResolutionTimeMinutes,
                    EscalateAfterMinutes = priority.ResponseTimeMinutes,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureEscalationRulesAsync(HelpDeskDbContext context, CancellationToken cancellationToken)
    {
        var priorities = await context.TicketPriorities.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var priority in priorities)
        {
            var exists = await context.EscalationRules
                .AnyAsync(r => r.PriorityId == priority.Id && !r.IsDeleted, cancellationToken);
            if (exists)
                continue;

            context.EscalationRules.Add(new EscalationRule
            {
                PriorityId = priority.Id,
                EscalateAfterMinutes = Math.Max(5, priority.ResponseTimeMinutes),
                EscalateToRole = "Admin",
                IsActive = true
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAutomationRulesAsync(HelpDeskDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.AutomationRules.AnyAsync(cancellationToken))
        {
            context.AutomationRules.AddRange(
                new AutomationRule
                {
                    Name = "Auto-assign new tickets to agents",
                    TriggerType = AutomationTriggerType.TicketCreated,
                    ActionType = AutomationActionType.AssignRole,
                    ActionValue = "Agent",
                    ExecutionOrder = 10,
                    IsActive = true
                },
                new AutomationRule
                {
                    Name = "Notify admins on escalation",
                    TriggerType = AutomationTriggerType.TicketStatusChanged,
                    ConditionStatus = TicketStatus.Escalated,
                    ActionType = AutomationActionType.NotifyRole,
                    ActionValue = "Admin",
                    ExecutionOrder = 20,
                    IsActive = true
                });

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsurePortalDefaultsAsync(HelpDeskDbContext context, int tenantId, CancellationToken cancellationToken)
    {
        if (!await context.TenantPortalSettings.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.TenantPortalSettings.Add(new TenantPortalSetting
            {
                TenantId = tenantId,
                BrandName = "Help Desk Portal",
                PrimaryColor = "#1F6FEB",
                WelcomeMessage = "How can we help you today?"
            });
        }

        if (!await context.BusinessHoursProfiles.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.BusinessHoursProfiles.Add(new BusinessHoursProfile
            {
                TenantId = tenantId,
                Name = "Default Business Hours",
                TimeZoneId = "UTC",
                WorkingDays = "1,2,3,4,5",
                StartLocalTime = new TimeOnly(8, 0, 0),
                EndLocalTime = new TimeOnly(17, 0, 0),
                IsDefault = true,
                IsActive = true
            });
        }

        if (!await context.KnowledgeBaseCategories.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.KnowledgeBaseCategories.AddRange(
                new KnowledgeBaseCategory
                {
                    TenantId = tenantId,
                    Name = "Getting Started",
                    Description = "Setup and onboarding guides",
                    IsPublic = true,
                    DisplayOrder = 10
                },
                new KnowledgeBaseCategory
                {
                    TenantId = tenantId,
                    Name = "Troubleshooting",
                    Description = "Common issues and fixes",
                    IsPublic = true,
                    DisplayOrder = 20
                });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureTenantSecurityPolicyAsync(HelpDeskDbContext context, int tenantId, CancellationToken cancellationToken)
    {
        if (await context.TenantSecurityPolicies.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
            return;

        context.TenantSecurityPolicies.Add(new TenantSecurityPolicy
        {
            TenantId = tenantId,
            RequireMfaForPrivilegedUsers = false,
            AllowedIpRanges = string.Empty,
            BlockInboundEmailTicketCreation = false,
            ScimBearerTokenHash = string.Empty
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureEnterpriseDefaultsAsync(HelpDeskDbContext context, int tenantId, CancellationToken cancellationToken)
    {
        if (!await context.IdentityProviderConfigs.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.IdentityProviderConfigs.Add(new IdentityProviderConfig
            {
                TenantId = tenantId,
                Name = "Default OIDC",
                Protocol = IdentityProtocol.Oidc,
                Issuer = "https://accounts.google.com",
                AuthorityOrMetadataUrl = "https://accounts.google.com",
                ClientId = "replace-me",
                ClientSecret = "replace-me",
                Audience = string.Empty,
                EnforceSso = false,
                EnforceStrictIssuer = true,
                AllowedRedirectUrisJson = "[\"https://localhost:3000/auth/callback\"]",
                OidcRequirePkce = true,
                IsEnabled = false
            });
        }

        if (!await context.AbacPolicyRules.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.AbacPolicyRules.Add(new AbacPolicyRule
            {
                TenantId = tenantId,
                Name = "Default Ticket Access",
                Resource = "ticket",
                Action = "read_write",
                ConditionJson = "{\"roles\":[\"Admin\",\"Agent\"]}",
                Effect = PolicyEffect.Allow,
                Priority = 100,
                IsEnabled = true
            });
        }

        if (!await context.OmnichannelConnectors.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.OmnichannelConnectors.Add(new OmnichannelConnector
            {
                TenantId = tenantId,
                ChannelType = ChannelType.WebForm,
                Name = "Default Web Form",
                ProviderKey = "portal",
                ConfigJson = "{\"source\":\"portal\"}",
                InboundSigningSecretHash = string.Empty,
                SignatureHeaderName = "X-Channel-Signature",
                SignatureAlgorithm = "hmac-sha256",
                DedupWindowMinutes = 120,
                Status = ConnectorStatus.Enabled
            });
        }

        if (!await context.WorkflowDefinitions.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.WorkflowDefinitions.Add(new WorkflowDefinition
            {
                TenantId = tenantId,
                Name = "Default Escalation Workflow",
                Version = 1,
                IsPublished = true,
                GraphJson = "{\"nodes\":[{\"id\":\"start\",\"type\":\"start\"},{\"id\":\"branch-1\",\"type\":\"branch\"},{\"id\":\"guard-1\",\"type\":\"guard\"},{\"id\":\"delay-1\",\"type\":\"delay\",\"delayMinutes\":60}],\"edges\":[{\"from\":\"start\",\"to\":\"branch-1\",\"condition\":\"always\",\"isDefault\":true},{\"from\":\"branch-1\",\"to\":\"guard-1\",\"condition\":\"priority=high\",\"isDefault\":false},{\"from\":\"branch-1\",\"to\":\"delay-1\",\"condition\":\"default\",\"isDefault\":true}]}",
                GuardrailJson = "{\"maxExecutionMinutes\":240,\"maxLoopCount\":3,\"requiresGuard\":true}",
                MaxLoopCount = 3
            });
        }

        if (!await context.IntegrationApps.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.IntegrationApps.Add(new IntegrationApp
            {
                TenantId = tenantId,
                Name = "Slack Integration (Disabled)",
                Provider = "slack",
                ConfigJson = "{\"webhookUrl\":\"\"}",
                IsEnabled = false
            });
        }

        if (!await context.WebhookSubscriptions.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.WebhookSubscriptions.Add(new WebhookSubscription
            {
                TenantId = tenantId,
                Name = "Audit Webhook",
                EndpointUrl = "https://example.com/webhooks/helpdesk",
                SecretHash = string.Empty,
                EventFiltersJson = "[\"ticket.created\",\"ticket.updated\",\"ticket.closed\"]",
                MaxAttempts = 5,
                RetryBackoffSeconds = 30,
                TimeoutSeconds = 20,
                IsEnabled = false
            });
        }

        if (!await context.MarketplaceApps.AnyAsync(x => !x.IsDeleted, cancellationToken))
        {
            context.MarketplaceApps.AddRange(
                new MarketplaceApp
                {
                    AppKey = "slack.notifications",
                    Name = "Slack Notifications",
                    Category = "communication",
                    Provider = "slack",
                    ManifestJson = "{\"events\":[\"ticket.created\",\"ticket.updated\"],\"requires\":[\"webhookUrl\"]}",
                    MinPlanName = "Professional",
                    IsPublic = true,
                    IsActive = true
                },
                new MarketplaceApp
                {
                    AppKey = "teams.notifications",
                    Name = "Microsoft Teams Notifications",
                    Category = "communication",
                    Provider = "teams",
                    ManifestJson = "{\"events\":[\"ticket.escalated\"],\"requires\":[\"webhookUrl\"]}",
                    MinPlanName = "Professional",
                    IsPublic = true,
                    IsActive = true
                });
        }

        if (!await context.BillingPlans.AnyAsync(x => !x.IsDeleted, cancellationToken))
        {
            context.BillingPlans.AddRange(
                new BillingPlan
                {
                    Name = "Starter",
                    MonthlyPriceUsd = 49,
                    IncludedAgentSeats = 5,
                    IncludedTicketsPerMonth = 1000,
                    EntitlementsJson = "{\"channels\":[\"web\"],\"automation\":false}",
                    IsActive = true
                },
                new BillingPlan
                {
                    Name = "Professional",
                    MonthlyPriceUsd = 149,
                    IncludedAgentSeats = 25,
                    IncludedTicketsPerMonth = 10000,
                    EntitlementsJson = "{\"channels\":[\"web\",\"email\",\"chat\"],\"automation\":true}",
                    IsActive = true
                },
                new BillingPlan
                {
                    Name = "Enterprise",
                    MonthlyPriceUsd = 499,
                    IncludedAgentSeats = 250,
                    IncludedTicketsPerMonth = 250000,
                    EntitlementsJson = "{\"channels\":[\"web\",\"email\",\"chat\",\"voice\"],\"automation\":true,\"sso\":true,\"scim\":true}",
                    IsActive = true
                });
        }

        await context.SaveChangesAsync(cancellationToken);

        if (!await context.TenantSubscriptions.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            var starter = await context.BillingPlans
                .Where(x => x.Name == "Starter" && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (starter != null)
            {
                context.TenantSubscriptions.Add(new TenantSubscription
                {
                    TenantId = tenantId,
                    BillingPlanId = starter.Id,
                    Status = SubscriptionStatus.Trial,
                    CurrentPeriodStartUtc = DateTime.UtcNow.Date,
                    CurrentPeriodEndUtc = DateTime.UtcNow.Date.AddDays(14),
                    AutoRenew = false,
                    EntitlementOverridesJson = "{}"
                });
            }
        }

        if (!await context.ServiceProjects.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.ServiceProjects.Add(new ServiceProject
            {
                TenantId = tenantId,
                Key = "IT",
                Name = "IT Service",
                WorkflowConfigJson = "{\"workflow\":\"default\"}"
            });
        }

        if (!await context.SlaPauseRules.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.SlaPauseRules.Add(new SlaPauseRule
            {
                TenantId = tenantId,
                Name = "Pause while waiting for customer",
                ConditionJson = "{\"status\":\"WaitingForCustomer\"}",
                PauseResponseSla = false,
                PauseResolutionSla = true,
                IsEnabled = true
            });
        }

        if (!await context.SlaBreachActions.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken))
        {
            context.SlaBreachActions.AddRange(
                new SlaBreachAction
                {
                    TenantId = tenantId,
                    Name = "Notify admins on resolution breach",
                    BreachType = "resolution",
                    TriggerAfterBreachMinutes = 0,
                    ExecutionOrder = 10,
                    ActionType = "notify_role",
                    ActionConfigJson = "{\"role\":\"Admin\"}",
                    IsEnabled = true
                },
                new SlaBreachAction
                {
                    TenantId = tenantId,
                    Name = "Escalate status on prolonged breach",
                    BreachType = "resolution",
                    TriggerAfterBreachMinutes = 60,
                    ExecutionOrder = 20,
                    ActionType = "set_status",
                    ActionConfigJson = "{\"status\":\"Escalated\"}",
                    IsEnabled = true
                });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
