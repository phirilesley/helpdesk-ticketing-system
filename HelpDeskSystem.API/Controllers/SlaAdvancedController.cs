using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/sla/advanced")]
public class SlaAdvancedController : ControllerBase
{
    private readonly HelpDeskDbContext _context;
    private readonly ISlaService _slaService;

    public SlaAdvancedController(HelpDeskDbContext context, ISlaService slaService)
    {
        _context = context;
        _slaService = slaService;
    }

    [HttpGet("pause-rules")]
    public async Task<ActionResult<IEnumerable<SlaPauseRule>>> GetPauseRules([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var items = await _context.SlaPauseRules
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(items);
    }

    [HttpPost("pause-rules")]
    public async Task<ActionResult<SlaPauseRule>> UpsertPauseRule([FromBody] UpsertSlaPauseRuleRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        SlaPauseRule entity;
        if (request.Id.HasValue)
        {
            entity = await _context.SlaPauseRules
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new SlaPauseRule { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new SlaPauseRule { TenantId = resolvedTenantId.Value };
            _context.SlaPauseRules.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.ConditionJson = request.ConditionJson.Trim();
        entity.PauseResponseSla = request.PauseResponseSla;
        entity.PauseResolutionSla = request.PauseResolutionSla;
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.SlaPauseRules.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("breach-actions")]
    public async Task<ActionResult<IEnumerable<SlaBreachAction>>> GetBreachActions([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var items = await _context.SlaBreachActions
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderBy(x => x.ExecutionOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(items);
    }

    [HttpPost("breach-actions")]
    public async Task<ActionResult<SlaBreachAction>> UpsertBreachAction([FromBody] UpsertSlaBreachActionRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        SlaBreachAction entity;
        if (request.Id.HasValue)
        {
            entity = await _context.SlaBreachActions
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new SlaBreachAction { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new SlaBreachAction { TenantId = resolvedTenantId.Value };
            _context.SlaBreachActions.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.BreachType = request.BreachType.Trim().ToLowerInvariant();
        entity.TriggerAfterBreachMinutes = request.TriggerAfterBreachMinutes < 0 ? 0 : request.TriggerAfterBreachMinutes;
        entity.ExecutionOrder = request.ExecutionOrder <= 0 ? 10 : request.ExecutionOrder;
        entity.ActionType = request.ActionType.Trim().ToLowerInvariant();
        entity.ActionConfigJson = request.ActionConfigJson.Trim();
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.SlaBreachActions.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpPost("simulate/{ticketId:int}")]
    [Authorize(Roles = "Admin,SuperAdmin,Agent")]
    public async Task<ActionResult> Simulate(int ticketId)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ticketId && !x.IsDeleted, HttpContext.RequestAborted);
        if (ticket == null) return NotFound();

        var sla = await _slaService.CalculateSlaForTicketAsync(ticketId);
        var actions = await _context.SlaBreachActions
            .AsNoTracking()
            .Where(x => x.TenantId == ticket.TenantId && x.IsEnabled && !x.IsDeleted)
            .OrderBy(x => x.ExecutionOrder)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(new
        {
            ticketId,
            sla,
            matchedActions = actions
                .Where(x => (x.BreachType == "response" && sla.IsResponseBreached) || (x.BreachType == "resolution" && sla.IsResolutionBreached))
                .Select(x => new { x.Id, x.Name, x.BreachType, x.TriggerAfterBreachMinutes, x.ActionType, x.ActionConfigJson })
        });
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }
}

public class UpsertSlaPauseRuleRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConditionJson { get; set; } = "{}";
    public bool PauseResponseSla { get; set; } = true;
    public bool PauseResolutionSla { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
}

public class UpsertSlaBreachActionRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BreachType { get; set; } = "resolution";
    public int TriggerAfterBreachMinutes { get; set; }
    public int ExecutionOrder { get; set; } = 10;
    public string ActionType { get; set; } = "notify_role";
    public string ActionConfigJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
}
