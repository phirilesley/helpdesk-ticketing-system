using HelpDeskSystem.Persistence.Context;
using HelpDeskSystem.Workflow.Visual.Models;
using HelpDeskSystem.Workflow.Visual.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HelpDeskSystem.Tests;

public class VisualWorkflowEngineTests
{
    [Fact]
    public async Task CreateAndGetWorkflow_RoundTripsGraph()
    {
        await using var context = CreateDbContext();
        var engine = new VisualWorkflowEngine(context, NullLogger<VisualWorkflowEngine>.Instance);

        var created = await engine.CreateWorkflowAsync(new WorkflowDefinition
        {
            TenantId = 1,
            Name = "Escalation Flow",
            IsActive = true,
            Version = 1,
            Nodes =
            [
                new WorkflowNode { Id = "start", Type = WorkflowNodeTypes.Start, Name = "Start", IsStartNode = true },
                new WorkflowNode { Id = "end", Type = WorkflowNodeTypes.End, Name = "End", IsEndNode = true }
            ],
            Connections =
            [
                new WorkflowConnection { Id = "c1", SourceNodeId = "start", TargetNodeId = "end", Type = "default", IsActive = true }
            ],
            Variables = [],
            Settings = new WorkflowSettings()
        });

        var fetched = await engine.GetWorkflowAsync(created.Id);

        Assert.Equal("Escalation Flow", fetched.Name);
        Assert.Equal(2, fetched.Nodes.Length);
        Assert.Single(fetched.Connections);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_CompletesExecution()
    {
        await using var context = CreateDbContext();
        var engine = new VisualWorkflowEngine(context, NullLogger<VisualWorkflowEngine>.Instance);

        var created = await engine.CreateWorkflowAsync(new WorkflowDefinition
        {
            TenantId = 1,
            Name = "Run Flow",
            IsActive = true,
            Version = 1,
            Nodes = [new WorkflowNode { Id = "start", Type = WorkflowNodeTypes.Start, Name = "Start", IsStartNode = true }],
            Connections = [],
            Variables = [],
            Settings = new WorkflowSettings()
        });

        var execution = await engine.ExecuteWorkflowAsync(created.Id, new WorkflowExecutionContext
        {
            ExecutionId = Guid.NewGuid().ToString("N"),
            WorkflowId = created.Id,
            TenantId = 1,
            Variables = new Dictionary<string, object>(),
            InputData = new Dictionary<string, object>(),
            OutputData = new Dictionary<string, object>(),
            Metadata = new Dictionary<string, object>(),
            TriggeredBy = "test-user",
            StartedAt = DateTime.UtcNow,
            User = new WorkflowUser { Id = "1", Name = "Test User", Email = "test@example.com", Role = "Admin", Properties = new Dictionary<string, object>() },
            Ticket = new WorkflowTicket { Id = 100, Number = "HD-100", Title = "Title", Description = "Desc", Status = "Open", Priority = "High", Category = "General", CreatedById = 1, CreatedAt = DateTime.UtcNow, CustomFields = new Dictionary<string, object>() }
        });

        Assert.Equal(WorkflowExecutionStatus.Completed, execution.Status);
        Assert.NotNull(execution.CompletedAt);
        Assert.NotEmpty(execution.Steps);
    }

    private static HelpDeskDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HelpDeskDbContext>()
            .UseInMemoryDatabase($"visual-workflow-tests-{Guid.NewGuid():N}")
            .Options;
        return new HelpDeskDbContext(options);
    }
}
