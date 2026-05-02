using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("scim/v2/Users")]
public class ScimUsersController : ControllerBase
{
    private readonly HelpDeskDbContext _context;
    private readonly ITenantSecurityPolicyService _tenantSecurityPolicyService;
    private readonly IAuditService _auditService;

    public ScimUsersController(
        HelpDeskDbContext context,
        ITenantSecurityPolicyService tenantSecurityPolicyService,
        IAuditService auditService)
    {
        _context = context;
        _tenantSecurityPolicyService = tenantSecurityPolicyService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? filter = null)
    {
        var auth = await AuthorizeScimRequestAsync();
        if (!auth.Authorized)
            return Unauthorized();

        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.TenantId == auth.TenantId && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var parsed = TryParseUserNameFilter(filter);
            if (!string.IsNullOrWhiteSpace(parsed))
                query = query.Where(u => u.Email == parsed);
        }

        var users = await query
            .OrderBy(u => u.Id)
            .Select(u => ToScimResource(u))
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(new
        {
            totalResults = users.Count,
            itemsPerPage = users.Count,
            startIndex = 1,
            Resources = users
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ScimUserRequest request)
    {
        var auth = await AuthorizeScimRequestAsync();
        if (!auth.Authorized)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest("userName is required.");

        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.UserName && u.TenantId == auth.TenantId, HttpContext.RequestAborted);
        if (existing != null)
            return Conflict();

        var entity = new User
        {
            Username = request.UserName.Split('@')[0],
            Email = request.UserName.Trim(),
            FirstName = request.Name?.GivenName?.Trim() ?? string.Empty,
            LastName = request.Name?.FamilyName?.Trim() ?? string.Empty,
            IsActive = request.Active ?? true,
            TenantId = auth.TenantId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerateTemporaryPassword())
        };

        _context.Users.Add(entity);
        await _context.SaveChangesAsync(HttpContext.RequestAborted);

        var customerRoleId = await _context.Roles
            .Where(r => r.Name == "Customer")
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(HttpContext.RequestAborted);
        if (customerRoleId.HasValue &&
            !await _context.UserRoles.AnyAsync(ur => ur.UserId == entity.Id && ur.RoleId == customerRoleId.Value, HttpContext.RequestAborted))
        {
            _context.UserRoles.Add(new UserRole { UserId = entity.Id, RoleId = customerRoleId.Value });
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
        }

        await _auditService.LogAsync(
            null,
            "SCIM_USER_CREATED",
            "User",
            entity.Id.ToString(),
            newValues: $"{{\"tenantId\":{auth.TenantId},\"email\":\"{entity.Email}\"}}",
            cancellationToken: HttpContext.RequestAborted);

        return Created($"/scim/v2/Users/{entity.Id}", ToScimResource(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Replace(int id, [FromBody] ScimUserRequest request)
    {
        var auth = await AuthorizeScimRequestAsync();
        if (!auth.Authorized)
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == auth.TenantId, HttpContext.RequestAborted);
        if (user == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            user.Email = request.UserName.Trim();
            user.Username = request.UserName.Split('@')[0];
        }
        user.FirstName = request.Name?.GivenName?.Trim() ?? user.FirstName;
        user.LastName = request.Name?.FamilyName?.Trim() ?? user.LastName;
        if (request.Active.HasValue)
            user.IsActive = request.Active.Value;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(HttpContext.RequestAborted);

        await _auditService.LogAsync(
            null,
            "SCIM_USER_UPDATED",
            "User",
            user.Id.ToString(),
            newValues: $"{{\"tenantId\":{auth.TenantId},\"active\":{user.IsActive.ToString().ToLowerInvariant()}}}",
            cancellationToken: HttpContext.RequestAborted);

        return Ok(ToScimResource(user));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(int id, [FromBody] ScimPatchRequest request)
    {
        var auth = await AuthorizeScimRequestAsync();
        if (!auth.Authorized)
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == auth.TenantId, HttpContext.RequestAborted);
        if (user == null)
            return NotFound();

        foreach (var op in request.Operations ?? [])
        {
            if (!string.Equals(op.Op, "replace", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(op.Path, "active", StringComparison.OrdinalIgnoreCase) && bool.TryParse(op.Value?.ToString(), out var active))
            {
                user.IsActive = active;
            }
        }
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(HttpContext.RequestAborted);

        await _auditService.LogAsync(
            null,
            "SCIM_USER_PATCHED",
            "User",
            user.Id.ToString(),
            newValues: $"{{\"tenantId\":{auth.TenantId},\"active\":{user.IsActive.ToString().ToLowerInvariant()}}}",
            cancellationToken: HttpContext.RequestAborted);

        return Ok(ToScimResource(user));
    }

    private async Task<(bool Authorized, int TenantId)> AuthorizeScimRequestAsync()
    {
        var tenantDomain = Request.Headers["X-Tenant-Domain"].ToString();
        if (string.IsNullOrWhiteSpace(tenantDomain))
            return (false, 0);

        var bearer = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(bearer) || !bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return (false, 0);
        var token = bearer["Bearer ".Length..].Trim();

        var tenantId = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Domain == tenantDomain && t.IsActive)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(HttpContext.RequestAborted);
        if (!tenantId.HasValue)
            return (false, 0);

        var valid = await _tenantSecurityPolicyService.ValidateScimTokenAsync(tenantId.Value, token, HttpContext.RequestAborted);
        return (valid, tenantId.Value);
    }

    private static string TryParseUserNameFilter(string filter)
    {
        var token = "userName eq ";
        if (!filter.StartsWith(token, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var value = filter[token.Length..].Trim();
        if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
            value = value[1..^1];
        return value;
    }

    private static object ToScimResource(User user)
    {
        return new
        {
            id = user.Id.ToString(),
            userName = user.Email,
            active = user.IsActive,
            name = new
            {
                givenName = user.FirstName,
                familyName = user.LastName
            }
        };
    }

    private static string GenerateTemporaryPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes);
    }
}

public class ScimUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public bool? Active { get; set; }
    public ScimUserName? Name { get; set; }
}

public class ScimUserName
{
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
}

public class ScimPatchRequest
{
    public List<ScimPatchOperation> Operations { get; set; } = [];
}

public class ScimPatchOperation
{
    public string Op { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public object? Value { get; set; }
}
