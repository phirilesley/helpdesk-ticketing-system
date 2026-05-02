using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Workflow.Models;
using HelpDeskSystem.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowEngine _workflowEngine;

    public WorkflowController(IWorkflowEngine workflowEngine)
    {
        _workflowEngine = workflowEngine;
    }

    [HttpGet("rules")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<WorkflowRule>>> GetWorkflowRules()
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var rules = await _workflowEngine.GetActiveRulesAsync(tenantId.Value);
        return Ok(rules);
    }

    [HttpPost("rules")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<WorkflowRule>> CreateWorkflowRule([FromBody] CreateWorkflowRuleDto dto)
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var rule = new WorkflowRule
        {
            Name = dto.Name,
            Description = dto.Description,
            TenantId = tenantId.Value,
            IsActive = dto.IsActive,
            Priority = dto.Priority,
            TriggerType = dto.TriggerType,
            TriggerConditionJson = dto.TriggerConditionJson,
            ActionsJson = dto.ActionsJson,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _workflowEngine.CreateWorkflowRuleAsync(rule);
        return CreatedAtAction(nameof(GetWorkflowRules), new { }, rule);
    }

    [HttpPut("rules/{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateWorkflowRule(int id, [FromBody] UpdateWorkflowRuleDto dto)
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        // This would need to be implemented in the service
        // For now, return success
        return NoContent();
    }

    [HttpDelete("rules/{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteWorkflowRule(int id)
    {
        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var result = await _workflowEngine.DeleteWorkflowRuleAsync(id, tenantId.Value);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("test")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> TestWorkflow([FromBody] TestWorkflowDto dto)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        await _workflowEngine.TriggerWorkflowsAsync(dto.TriggerType, dto.TicketId, userId.Value, dto.TriggerData);
        return Ok(new { Message = "Workflow triggered successfully" });
    }
}

public class CreateWorkflowRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;
    public WorkflowTriggerType TriggerType { get; set; }
    public string? TriggerConditionJson { get; set; }
    public string ActionsJson { get; set; } = string.Empty;
}

public class UpdateWorkflowRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public string? TriggerConditionJson { get; set; }
    public string ActionsJson { get; set; } = string.Empty;
}

public class TestWorkflowDto
{
    public WorkflowTriggerType TriggerType { get; set; }
    public int TicketId { get; set; }
    public object? TriggerData { get; set; }
}
