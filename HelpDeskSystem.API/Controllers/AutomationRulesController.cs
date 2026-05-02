using HelpDeskSystem.Application.DTOs.Workflow;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/[controller]")]
public class AutomationRulesController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public AutomationRulesController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AutomationRuleDto>>> GetRules([FromQuery] int? tenantId = null)
    {
        var query = _context.AutomationRules.AsQueryable();
        if (tenantId.HasValue)
        {
            query = query.Where(r => r.TenantId == tenantId);
        }

        var rules = await query
            .OrderBy(r => r.ExecutionOrder)
            .Select(r => new AutomationRuleDto
            {
                Id = r.Id,
                Name = r.Name,
                TenantId = r.TenantId,
                TriggerType = r.TriggerType,
                ConditionCategoryId = r.ConditionCategoryId,
                ConditionPriorityId = r.ConditionPriorityId,
                ConditionStatus = r.ConditionStatus,
                ActionType = r.ActionType,
                ActionValue = r.ActionValue,
                ExecutionOrder = r.ExecutionOrder,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost]
    public async Task<ActionResult<AutomationRuleDto>> CreateRule(UpsertAutomationRuleDto dto)
    {
        var entity = new AutomationRule
        {
            Name = dto.Name,
            TenantId = dto.TenantId,
            TriggerType = dto.TriggerType,
            ConditionCategoryId = dto.ConditionCategoryId,
            ConditionPriorityId = dto.ConditionPriorityId,
            ConditionStatus = dto.ConditionStatus,
            ActionType = dto.ActionType,
            ActionValue = dto.ActionValue,
            ExecutionOrder = dto.ExecutionOrder,
            IsActive = dto.IsActive
        };

        _context.AutomationRules.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRuleById), new { id = entity.Id }, ToDto(entity));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AutomationRuleDto>> GetRuleById(int id)
    {
        var entity = await _context.AutomationRules.FindAsync(id);
        if (entity == null)
            return NotFound();

        return Ok(ToDto(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRule(int id, UpsertAutomationRuleDto dto)
    {
        var entity = await _context.AutomationRules.FindAsync(id);
        if (entity == null)
            return NotFound();

        entity.Name = dto.Name;
        entity.TenantId = dto.TenantId;
        entity.TriggerType = dto.TriggerType;
        entity.ConditionCategoryId = dto.ConditionCategoryId;
        entity.ConditionPriorityId = dto.ConditionPriorityId;
        entity.ConditionStatus = dto.ConditionStatus;
        entity.ActionType = dto.ActionType;
        entity.ActionValue = dto.ActionValue;
        entity.ExecutionOrder = dto.ExecutionOrder;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var entity = await _context.AutomationRules.FindAsync(id);
        if (entity == null)
            return NotFound();

        _context.AutomationRules.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static AutomationRuleDto ToDto(AutomationRule rule)
    {
        return new AutomationRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            TenantId = rule.TenantId,
            TriggerType = rule.TriggerType,
            ConditionCategoryId = rule.ConditionCategoryId,
            ConditionPriorityId = rule.ConditionPriorityId,
            ConditionStatus = rule.ConditionStatus,
            ActionType = rule.ActionType,
            ActionValue = rule.ActionValue,
            ExecutionOrder = rule.ExecutionOrder,
            IsActive = rule.IsActive
        };
    }
}
