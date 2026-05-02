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
}
