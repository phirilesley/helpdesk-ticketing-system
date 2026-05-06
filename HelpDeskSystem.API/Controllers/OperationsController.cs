using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/operations")]
public class OperationsController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public OperationsController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var last24h = now.AddHours(-24);

        var inboundBacklog = await _context.InboundChannelEvents
            .CountAsync(x =>
                !x.IsDeleted &&
                (x.Status == Domain.Enums.InboundEventStatus.Received || x.Status == Domain.Enums.InboundEventStatus.Normalized));

        var webhookPending = await _context.WebhookDeliveries
            .CountAsync(x => x.Status == "Pending" || x.Status == "Retrying");

        var webhookFailures24h = await _context.WebhookDeliveries
            .CountAsync(x => x.Status == "Failed" && x.CreatedAtUtc >= last24h);

        var openTickets = await _context.Tickets
            .CountAsync(x => !x.IsDeleted && x.Status != Domain.Enums.TicketStatus.Closed);

        var slaBreaches24h = await _context.SlaBreachLogs
            .CountAsync(x => !x.IsDeleted && x.BreachedAtUtc >= last24h);

        return Ok(new
        {
            generatedAtUtc = now,
            openTickets,
            inboundBacklog,
            webhookPending,
            webhookFailures24h,
            slaBreaches24h
        });
    }
}
