using HelpDeskSystem.API.Security;
using HelpDeskSystem.API.Services;
using HelpDeskSystem.Application.DTOs.Auth;
using HelpDeskSystem.Application.DTOs.Users;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ITenantSecurityPolicyService _tenantSecurityPolicyService;
    private readonly HelpDeskDbContext _context;

    public UsersController(
        IUserService userService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ITenantSecurityPolicyService tenantSecurityPolicyService,
        HelpDeskDbContext context)
    {
        _userService = userService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _tenantSecurityPolicyService = tenantSecurityPolicyService;
        _context = context;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        var result = await _userService.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();
        return user;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId.Value);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("email/{email}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
            return NotFound();
        return user;
    }

    [HttpGet("tenant/{tenantId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByTenant(int tenantId)
    {
        var users = await _userService.GetUsersByTenantAsync(tenantId);
        return Ok(users);
    }

    [HttpGet("assignable-agents")]
    [Authorize(Roles = "Agent,Admin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<AssignableUserDto>>> GetAssignableAgents()
    {
        if (User.IsInRole("SuperAdmin"))
        {
            var superUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Agent"))
                .Select(u => new AssignableUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = $"{u.FirstName} {u.LastName}".Trim()
                })
                .ToListAsync(HttpContext.RequestAborted);

            return Ok(superUsers);
        }

        var tenantId = User.GetTenantId();
        if (!tenantId.HasValue)
            return Forbid();

        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .Where(u => u.IsActive
                && u.TenantId == tenantId.Value
                && u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Agent"))
            .Select(u => new AssignableUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = $"{u.FirstName} {u.LastName}".Trim()
            })
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(users);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
    {
        await _userService.UpdateUserAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/roles/{roleId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AssignRole(int id, int roleId)
    {
        await _userService.AssignRoleToUserAsync(id, roleId);
        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var isValid = await _userService.ValidatePasswordAsync(dto.Email, dto.Password);
        if (!isValid)
            return Unauthorized();

        var user = await _userService.GetUserByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized();

        if (user.TenantId.HasValue)
        {
            var ssoEnforced = await _context.IdentityProviderConfigs
                .AsNoTracking()
                .AnyAsync(x => x.TenantId == user.TenantId.Value && x.IsEnabled && x.EnforceSso && !x.IsDeleted, HttpContext.RequestAborted);
            if (ssoEnforced)
                return Unauthorized("Tenant enforces SSO. Use external identity provider login.");
        }

        if (user.TenantId.HasValue)
        {
            var ipAllowed = await _tenantSecurityPolicyService.IsIpAllowedAsync(
                user.TenantId.Value,
                GetIpAddress(),
                HttpContext.RequestAborted);
            if (!ipAllowed)
                return Unauthorized("IP address not allowed by tenant policy.");

            var requiresMfa = await _tenantSecurityPolicyService.RequiresMfaForUserAsync(
                user.TenantId.Value,
                user.Roles,
                HttpContext.RequestAborted);

            if (requiresMfa)
            {
                if (!user.IsMfaEnabled)
                    return Unauthorized("MFA enrollment required.");

                if (string.IsNullOrWhiteSpace(dto.MfaCode))
                    return Unauthorized("MFA code is required.");

                var validMfa = await _userService.VerifyMfaCodeAsync(user.Id, dto.MfaCode);
                if (!validMfa)
                    return Unauthorized("Invalid MFA code.");
            }
        }

        var accessToken = _tokenService.Generate(user);
        var (refreshToken, refreshExpiresAtUtc) = await _refreshTokenService.CreateAsync(
            user.Id,
            GetDeviceId(),
            GetDeviceName(),
            GetIpAddress(),
            GetUserAgent(),
            HttpContext.RequestAborted);

        return Ok(new LoginResponseDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc,
            User = user
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return Unauthorized();

        var rotated = await _refreshTokenService.RotateAsync(
            dto.RefreshToken,
            GetDeviceId(),
            GetDeviceName(),
            GetIpAddress(),
            GetUserAgent(),
            HttpContext.RequestAborted);

        if (rotated.Status == RefreshTokenRotateStatus.ReuseDetected)
            return Unauthorized("Refresh token reuse detected. Session family invalidated.");

        if (rotated.Status != RefreshTokenRotateStatus.Success)
            return Unauthorized();

        var user = await _userService.GetUserByIdAsync(rotated.UserId);
        if (user == null)
            return Unauthorized();

        var accessToken = _tokenService.Generate(user);

        return Ok(new LoginResponseDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = rotated.NewRefreshToken,
            RefreshTokenExpiresAtUtc = rotated.NewRefreshTokenExpiresAtUtc,
            User = user
        });
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest("Refresh token is required.");

        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var revoked = await _refreshTokenService.RevokeAsync(dto.RefreshToken, userId.Value, HttpContext.RequestAborted);
        if (!revoked)
            return NotFound();

        return NoContent();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<UserSessionInfo>>> GetSessions()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var sessions = await _refreshTokenService.GetSessionsAsync(userId.Value, HttpContext.RequestAborted);
        return Ok(sessions);
    }

    [HttpPost("sessions/{familyId}/revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeSessionFamily(string familyId)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var revoked = await _refreshTokenService.RevokeSessionFamilyAsync(userId.Value, familyId, HttpContext.RequestAborted);
        if (!revoked)
            return NotFound();

        return NoContent();
    }

    [HttpPost("sessions/revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        await _refreshTokenService.RevokeAllSessionsAsync(userId.Value, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("mfa/enroll")]
    [Authorize]
    public async Task<ActionResult<MfaEnrollmentDto>> EnrollMfa()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        var sharedSecret = await _userService.SetMfaSecretAsync(userId.Value);
        var issuer = Uri.EscapeDataString("HelpDeskSystem");
        var account = Uri.EscapeDataString(user.Email);
        var uri = $"otpauth://totp/{issuer}:{account}?secret={sharedSecret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";

        return Ok(new MfaEnrollmentDto
        {
            SharedSecret = sharedSecret,
            OtpauthUri = uri
        });
    }

    [HttpPost("mfa/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyRequestDto dto)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var enabled = await _userService.EnableMfaAsync(userId.Value, dto.Code);
        if (!enabled)
            return BadRequest("Invalid MFA code.");

        return NoContent();
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableMfa()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        await _userService.DisableMfaAsync(userId.Value);
        return NoContent();
    }

    private string GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private string GetUserAgent() => Request.Headers.UserAgent.ToString();

    private string GetDeviceId() => Request.Headers["X-Device-Id"].ToString();

    private string GetDeviceName() => Request.Headers["X-Device-Name"].ToString();
}

public class AssignableUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
