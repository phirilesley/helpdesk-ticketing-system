using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.DTOs.Sla;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/business-hours")]
public class BusinessHoursController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public BusinessHoursController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusinessHoursProfileDto>>> Get([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var profiles = await _context.BusinessHoursProfiles
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .Select(x => new BusinessHoursProfileDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Name = x.Name,
                TimeZoneId = x.TimeZoneId,
                WorkingDays = x.WorkingDays,
                StartLocalTime = x.StartLocalTime,
                EndLocalTime = x.EndLocalTime,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            })
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(profiles);
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] BusinessHoursProfileDto dto, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var entity = await _context.BusinessHoursProfiles
            .FirstOrDefaultAsync(x => x.Id == dto.Id && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted);

        if (entity == null)
        {
            entity = new Domain.Entities.BusinessHoursProfile
            {
                TenantId = resolvedTenantId.Value
            };
            _context.BusinessHoursProfiles.Add(entity);
        }

        entity.Name = dto.Name.Trim();
        entity.TimeZoneId = dto.TimeZoneId.Trim();
        entity.WorkingDays = dto.WorkingDays.Trim();
        entity.StartLocalTime = dto.StartLocalTime;
        entity.EndLocalTime = dto.EndLocalTime;
        entity.IsDefault = dto.IsDefault;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.IsDefault)
        {
            var others = await _context.BusinessHoursProfiles
                .Where(x => x.TenantId == resolvedTenantId.Value && x.Id != entity.Id && x.IsDefault && !x.IsDeleted)
                .ToListAsync(HttpContext.RequestAborted);
            foreach (var other in others)
            {
                other.IsDefault = false;
                other.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return NoContent();
    }

    private int? ResolveTenantId(int? tenantIdFromQuery)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantIdFromQuery ?? User.GetTenantId();

        return User.GetTenantId();
    }
}
