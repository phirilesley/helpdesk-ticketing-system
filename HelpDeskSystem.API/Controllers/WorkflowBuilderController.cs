using System.Text.Json;
using HelpDeskSystem.API.Security;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/workflow-builder")]
public class WorkflowBuilderController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public WorkflowBuilderController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet("definitions")]
    public async Task<ActionResult<IEnumerable<WorkflowDefinition>>> GetDefinitions([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var definitions = await _context.WorkflowDefinitions
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(definitions);
    }

    [HttpPost("definitions")]
    public async Task<ActionResult<WorkflowDefinition>> UpsertDefinition([FromBody] UpsertWorkflowDefinitionRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var validation = ValidateGraph(request.GraphJson);
        if (!validation.IsValid)
            return BadRequest(validation);

        WorkflowDefinition entity;
        if (request.Id.HasValue)
        {
            entity = await _context.WorkflowDefinitions
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new WorkflowDefinition { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new WorkflowDefinition { TenantId = resolvedTenantId.Value };
            _context.WorkflowDefinitions.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Version = request.Version <= 0 ? 1 : request.Version;
        entity.IsPublished = request.IsPublished;
        entity.GraphJson = request.GraphJson;
        entity.GuardrailJson = string.IsNullOrWhiteSpace(request.GuardrailJson) ? "{}" : request.GuardrailJson.Trim();
        entity.MaxLoopCount = request.MaxLoopCount <= 0 ? 3 : request.MaxLoopCount;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.WorkflowDefinitions.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpPost("validate")]
    public ActionResult<WorkflowGraphValidationResult> Validate([FromBody] ValidateWorkflowGraphRequest request)
    {
        return Ok(ValidateGraph(request.GraphJson));
    }

    private WorkflowGraphValidationResult ValidateGraph(string graphJson)
    {
        var result = new WorkflowGraphValidationResult { IsValid = true };
        try
        {
            var graph = JsonSerializer.Deserialize<WorkflowGraph>(graphJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (graph == null)
                return Fail("Graph payload is empty.");

            if (graph.Nodes.Count == 0)
                return Fail("At least one node is required.");

            if (graph.Nodes.Count > 150)
                return Fail("Graph exceeds max node count (150).");

            var validNodeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "start", "guard", "delay", "condition", "action", "branch", "end"
            };
            var invalidTypes = graph.Nodes
                .Where(x => !string.IsNullOrWhiteSpace(x.Type) && !validNodeTypes.Contains(x.Type))
                .Select(x => $"{x.Id}:{x.Type}")
                .ToList();
            if (invalidTypes.Count > 0)
                return Fail($"Unsupported node type(s): {string.Join(", ", invalidTypes)}");

            var nodeIds = graph.Nodes.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!nodeIds.Contains("start"))
                return Fail("Graph must include a 'start' node.");

            var totalDelayMinutes = graph.Nodes
                .Where(x => string.Equals(x.Type, "delay", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.DelayMinutes < 0 ? 0 : x.DelayMinutes)
                .Sum();
            if (totalDelayMinutes > 10080)
                return Fail("Total delay exceeds 10080 minutes (7 days).");

            foreach (var edge in graph.Edges)
            {
                if (!nodeIds.Contains(edge.From) || !nodeIds.Contains(edge.To))
                    return Fail($"Edge references unknown node: {edge.From} -> {edge.To}");
            }

            var outgoing = graph.Edges
                .GroupBy(x => x.From, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var branch in graph.Nodes.Where(x => string.Equals(x.Type, "branch", StringComparison.OrdinalIgnoreCase)))
            {
                if (!outgoing.TryGetValue(branch.Id, out var branchEdges) || branchEdges.Count < 2)
                    return Fail($"Branch node '{branch.Id}' must have at least 2 outgoing edges.");

                if (branchEdges.Count(e => e.IsDefault) != 1)
                    return Fail($"Branch node '{branch.Id}' must have exactly one default branch.");
            }

            var guardNodes = graph.Nodes.Count(x => string.Equals(x.Type, "guard", StringComparison.OrdinalIgnoreCase));
            if (ContainsCycle(graph))
            {
                if (guardNodes == 0)
                    return Fail("Graph contains a cycle without any guard node.");
                result.Warnings.Add("Graph contains cycle(s). Ensure max loop governance is configured and execution logs are monitored.");
            }

            if (guardNodes == 0)
                result.Warnings.Add("No guard node configured. Add at least one guardrail condition.");

            return result;
        }
        catch (Exception ex)
        {
            return Fail($"Invalid graph JSON: {ex.Message}");
        }

        WorkflowGraphValidationResult Fail(string error)
        {
            return new WorkflowGraphValidationResult
            {
                IsValid = false,
                Errors = { error }
            };
        }
    }

    private static bool ContainsCycle(WorkflowGraph graph)
    {
        var adj = graph.Edges
            .GroupBy(x => x.From, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.To).ToList(), StringComparer.OrdinalIgnoreCase);

        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool Dfs(string node)
        {
            if (visiting.Contains(node)) return true;
            if (visited.Contains(node)) return false;
            visiting.Add(node);

            if (adj.TryGetValue(node, out var children))
            {
                foreach (var child in children)
                {
                    if (Dfs(child)) return true;
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }

        return graph.Nodes.Any(n => Dfs(n.Id));
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }
}

public class UpsertWorkflowDefinitionRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsPublished { get; set; }
    public string GraphJson { get; set; } = "{\"nodes\":[],\"edges\":[]}";
    public string GuardrailJson { get; set; } = "{}";
    public int MaxLoopCount { get; set; } = 3;
}

public class ValidateWorkflowGraphRequest
{
    public string GraphJson { get; set; } = "{\"nodes\":[],\"edges\":[]}";
}

public class WorkflowGraphValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class WorkflowGraph
{
    public List<WorkflowGraphNode> Nodes { get; set; } = [];
    public List<WorkflowGraphEdge> Edges { get; set; } = [];
}

public class WorkflowGraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int DelayMinutes { get; set; }
}

public class WorkflowGraphEdge
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
