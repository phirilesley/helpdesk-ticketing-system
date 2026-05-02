using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/jobs")]
public class AdminJobsController : ControllerBase
{
    [HttpGet("summary")]
    public ActionResult<JobSummaryResponse> GetSummary()
    {
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var statistics = monitoringApi.GetStatistics();
        var queues = monitoringApi.Queues();

        return Ok(new JobSummaryResponse
        {
            Enqueued = statistics.Enqueued,
            Scheduled = statistics.Scheduled,
            Processing = statistics.Processing,
            Succeeded = statistics.Succeeded,
            Failed = statistics.Failed,
            Deleted = statistics.Deleted,
            Recurring = statistics.Recurring,
            Retries = statistics.Retries ?? 0,
            Queues = queues
                .Select(q => new QueueSummaryItem
                {
                    Name = q.Name,
                    Length = q.Length,
                    Fetched = q.Fetched ?? 0
                })
                .ToList()
        });
    }
}

public class JobSummaryResponse
{
    public long Enqueued { get; set; }
    public long Scheduled { get; set; }
    public long Processing { get; set; }
    public long Succeeded { get; set; }
    public long Failed { get; set; }
    public long Deleted { get; set; }
    public long Recurring { get; set; }
    public long Retries { get; set; }
    public List<QueueSummaryItem> Queues { get; set; } = [];
}

public class QueueSummaryItem
{
    public string Name { get; set; } = string.Empty;
    public long Length { get; set; }
    public long Fetched { get; set; }
}
