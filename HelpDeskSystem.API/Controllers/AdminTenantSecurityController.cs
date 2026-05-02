using HelpDeskSystem.API.Security;
using HelpDeskSystem.Application.DTOs.Security;
using HelpDeskSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/tenant-security")]
public class AdminTenantSecurityController : ControllerBase
{
    private readonly ITenantSecurityPolicyService _tenantSecurityPolicyService;

    public AdminTenantSecurityController(ITenantSecurityPolicyService tenantSecurityPolicyService)
    {
        _tenantSecurityPolicyService = tenantSecurityPolicyService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantSecurityPolicyDto>> GetPolicy([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var policy = await _tenantSecurityPolicyService.GetPolicyAsync(resolvedTenantId.Value, HttpContext.RequestAborted);
        return Ok(policy);
    }

    [HttpPut]
    public async Task<ActionResult<TenantSecurityPolicyDto>> UpsertPolicy(
        [FromBody] UpsertTenantSecurityPolicyDto dto,
        [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue)
            return Forbid();

        var policy = await _tenantSecurityPolicyService.UpsertPolicyAsync(resolvedTenantId.Value, dto, HttpContext.RequestAborted);
        return Ok(policy);
    }

    private int? ResolveTenantId(int? tenantIdFromQuery)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantIdFromQuery ?? User.GetTenantId();

        return User.GetTenantId();
    }
}
