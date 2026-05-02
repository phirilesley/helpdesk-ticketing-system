using HelpDeskSystem.API.Security;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/compliance")]
public class ComplianceController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public ComplianceController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet("legal-holds")]
    public async Task<ActionResult<IEnumerable<LegalHoldCase>>> GetLegalHolds([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var items = await _context.LegalHoldCases
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(items);
    }

    [HttpPost("legal-holds")]
    public async Task<ActionResult<LegalHoldCase>> UpsertLegalHold([FromBody] UpsertLegalHoldRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        LegalHoldCase entity;
        if (request.Id.HasValue)
        {
            entity = await _context.LegalHoldCases
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new LegalHoldCase { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new LegalHoldCase { TenantId = resolvedTenantId.Value };
            _context.LegalHoldCases.Add(entity);
        }

        entity.CaseNumber = request.CaseNumber.Trim();
        entity.Name = request.Name.Trim();
        entity.ScopeJson = request.ScopeJson.Trim();
        entity.ExpiresAtUtc = request.ExpiresAtUtc;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.LegalHoldCases.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpGet("dsr")]
    public async Task<ActionResult<IEnumerable<DataSubjectRequest>>> GetDataSubjectRequests([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var items = await _context.DataSubjectRequests
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(items);
    }

    [HttpPost("dsr")]
    public async Task<ActionResult<DataSubjectRequest>> UpsertDataSubjectRequest([FromBody] UpsertDataSubjectRequestRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        DataSubjectRequest entity;
        if (request.Id.HasValue)
        {
            entity = await _context.DataSubjectRequests
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new DataSubjectRequest { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new DataSubjectRequest
            {
                TenantId = resolvedTenantId.Value,
                ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber)
                    ? $"DSR-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    : request.ReferenceNumber.Trim()
            };
            _context.DataSubjectRequests.Add(entity);
        }

        entity.RequestType = request.RequestType;
        entity.Status = request.Status;
        entity.Stage = request.Stage;
        entity.SubjectEmail = request.SubjectEmail.Trim();
        entity.Notes = request.Notes.Trim();
        entity.CompletedAtUtc = request.Status == DataSubjectRequestStatus.Completed ? DateTime.UtcNow : null;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        await LogDsrStageAsync(entity, "DSR_UPDATED", request.Notes, User.GetUserId());
        return Ok(entity);
    }

    [HttpGet("dsr/{id:int}/logs")]
    public async Task<ActionResult<IEnumerable<DsrProcessingLog>>> GetDsrLogs(int id, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var dsr = await _context.DataSubjectRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted);
        if (dsr == null) return NotFound();

        var logs = await _context.DsrProcessingLogs
            .Where(x => x.DataSubjectRequestId == id && x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(logs);
    }

    [HttpPost("dsr/{id:int}/stage")]
    public async Task<ActionResult<DataSubjectRequest>> SetDsrStage(int id, [FromBody] SetDsrStageRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var dsr = await _context.DataSubjectRequests
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted);
        if (dsr == null) return NotFound();

        dsr.Stage = request.Stage;
        dsr.Status = request.Status;
        dsr.Notes = request.Notes.Trim();
        dsr.CompletedAtUtc = request.Status == DataSubjectRequestStatus.Completed ? DateTime.UtcNow : null;
        dsr.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        await LogDsrStageAsync(dsr, "DSR_STAGE_CHANGED", request.Notes, User.GetUserId());
        return Ok(dsr);
    }

    private async Task LogDsrStageAsync(DataSubjectRequest dsr, string action, string notes, int? userId)
    {
        _context.DsrProcessingLogs.Add(new DsrProcessingLog
        {
            TenantId = dsr.TenantId,
            DataSubjectRequestId = dsr.Id,
            Stage = dsr.Stage,
            Action = action,
            Notes = notes.Trim(),
            PerformedByUserId = userId
        });
        await _context.SaveChangesAsync(HttpContext.RequestAborted);
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }
}

public class UpsertLegalHoldRequest
{
    public int? Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ScopeJson { get; set; } = "{}";
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpsertDataSubjectRequestRequest
{
    public int? Id { get; set; }
    public DataSubjectRequestType RequestType { get; set; }
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.Open;
    public DsrProcessStage Stage { get; set; } = DsrProcessStage.Intake;
    public string SubjectEmail { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class SetDsrStageRequest
{
    public DsrProcessStage Stage { get; set; } = DsrProcessStage.Intake;
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.InProgress;
    public string Notes { get; set; } = string.Empty;
}
