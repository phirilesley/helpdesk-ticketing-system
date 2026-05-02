using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.DTOs.Portal;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Route("api/portal-settings")]
public class PortalSettingsController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public PortalSettingsController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<TenantPortalSettingDto>> Get([FromQuery] string? tenantDomain = null)
    {
        int? tenantId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            tenantId = User.GetTenantId();
        }

        if (!tenantId.HasValue && !string.IsNullOrWhiteSpace(tenantDomain))
        {
            tenantId = await _context.Tenants
                .Where(t => t.Domain == tenantDomain && t.IsActive)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
        }

        if (!tenantId.HasValue)
            return BadRequest("Tenant resolution failed.");

        var setting = await _context.TenantPortalSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && !x.IsDeleted, HttpContext.RequestAborted);

        if (setting == null)
        {
            return Ok(new TenantPortalSettingDto
            {
                TenantId = tenantId.Value,
                BrandName = "Help Desk Portal",
                PrimaryColor = "#1F6FEB",
                WelcomeMessage = "How can we help you today?"
            });
        }

        return Ok(new TenantPortalSettingDto
        {
            TenantId = setting.TenantId,
            BrandName = setting.BrandName,
            PrimaryColor = setting.PrimaryColor,
            LogoUrl = setting.LogoUrl,
            SupportEmail = setting.SupportEmail,
            WelcomeMessage = setting.WelcomeMessage
        });
    }

    [HttpPut]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Upsert([FromBody] TenantPortalSettingDto dto, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = User.IsInRole("SuperAdmin")
            ? tenantId ?? User.GetTenantId()
            : User.GetTenantId();

        if (!resolvedTenantId.HasValue)
            return Forbid();

        var setting = await _context.TenantPortalSettings
            .FirstOrDefaultAsync(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted);

        if (setting == null)
        {
            setting = new Domain.Entities.TenantPortalSetting
            {
                TenantId = resolvedTenantId.Value
            };
            _context.TenantPortalSettings.Add(setting);
        }

        setting.BrandName = dto.BrandName.Trim();
        setting.PrimaryColor = dto.PrimaryColor.Trim();
        setting.LogoUrl = dto.LogoUrl.Trim();
        setting.SupportEmail = dto.SupportEmail.Trim();
        setting.WelcomeMessage = dto.WelcomeMessage.Trim();
        setting.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return NoContent();
    }
}
