using HelpDeskSystem.API.Services;
using HelpDeskSystem.API.Security;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalAuthController : ControllerBase
    {
        private readonly IExternalAuthService _externalAuthService;
        private readonly HelpDeskDbContext _context;
        private readonly ILogger<ExternalAuthController> _logger;

        public ExternalAuthController(
            IExternalAuthService externalAuthService,
            HelpDeskDbContext context,
            ILogger<ExternalAuthController> logger)
        {
            _externalAuthService = externalAuthService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("providers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProviders([FromQuery] string tenantDomain, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(tenantDomain))
                return BadRequest(new { error = "tenantDomain is required." });

            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Domain == tenantDomain && t.IsActive && !t.IsDeleted, cancellationToken);

            if (tenant == null)
                return NotFound(new { error = "Tenant not found." });

            var providers = await _context.IdentityProviderConfigs
                .Where(p => p.TenantId == tenant.Id && p.IsEnabled && !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    Protocol = p.Protocol.ToString(),
                    p.Issuer,
                    p.EnforceSso
                })
                .ToListAsync(cancellationToken);

            return Ok(providers);
        }

        [HttpGet("providers/{providerId:int}/auth-url")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAuthUrl(
            int providerId,
            [FromQuery] string redirectUri,
            [FromQuery] string? codeChallenge,
            [FromQuery] string? state,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
                return BadRequest(new { error = "redirectUri is required." });

            try
            {
                var authUrl = await _externalAuthService.GenerateAuthUrlAsync(providerId, redirectUri, codeChallenge, state, cancellationToken);
                return Ok(new { authUrl });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate auth URL for provider {ProviderId}", providerId);
                return StatusCode(500, new { error = "Failed to generate authentication URL." });
            }
        }

        [HttpPost("oidc/{providerId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> AuthenticateWithOidc(int providerId, [FromBody] OidcAuthRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _externalAuthService.AuthenticateWithOidcAsync(
                    providerId,
                    request.Code,
                    request.RedirectUri,
                    request.DeviceId,
                    request.DeviceName,
                    ResolveClientIp(),
                    Request.Headers.UserAgent.ToString(),
                    cancellationToken);

                if (!result.Success)
                    return BadRequest(new { error = result.Error });

                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt,
                    user = new
                    {
                        result.User!.Id,
                        result.User.Email,
                        result.User.FirstName,
                        result.User.LastName,
                        result.User.Username,
                        roles = result.User.UserRoles
                            .Where(ur => ur.Role != null)
                            .Select(ur => ur.Role!.Name)
                            .ToArray()
                    },
                    metadata = result.Metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OIDC authentication failed for provider {ProviderId}", providerId);
                return StatusCode(500, new { error = "Authentication failed." });
            }
        }

        [HttpPost("saml/{providerId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> AuthenticateWithSaml(int providerId, [FromBody] SamlAuthRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _externalAuthService.AuthenticateWithSamlAsync(
                    providerId,
                    request.SamlResponse,
                    request.DeviceId,
                    request.DeviceName,
                    ResolveClientIp(),
                    Request.Headers.UserAgent.ToString(),
                    cancellationToken);

                if (!result.Success)
                    return BadRequest(new { error = result.Error });

                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt,
                    user = new
                    {
                        result.User!.Id,
                        result.User.Email,
                        result.User.FirstName,
                        result.User.LastName,
                        result.User.Username,
                        roles = result.User.UserRoles
                            .Where(ur => ur.Role != null)
                            .Select(ur => ur.Role!.Name)
                            .ToArray()
                    },
                    metadata = result.Metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAML authentication failed for provider {ProviderId}", providerId);
                return StatusCode(500, new { error = "Authentication failed." });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("user-info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var tenantId = User.GetTenantId();

            if (!userId.HasValue || !tenantId.HasValue)
                return Forbid();

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && u.TenantId == tenantId.Value && !u.IsDeleted, cancellationToken);

            if (user == null)
                return NotFound(new { error = "User not found." });

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Username,
                user.IsActive,
                user.IsMfaEnabled,
                Tenant = user.Tenant == null ? null : new
                {
                    user.Tenant.Id,
                    user.Tenant.Name,
                    user.Tenant.Domain
                },
                Roles = user.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => new
                {
                    ur.Role!.Id,
                    ur.Role.Name,
                    ur.Role.Description
                })
            });
        }

        private string ResolveClientIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public class OidcAuthRequest
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string DeviceId { get; set; } = "browser";
        public string DeviceName { get; set; } = "Browser";
    }

    public class SamlAuthRequest
    {
        public string SamlResponse { get; set; } = string.Empty;
        public string DeviceId { get; set; } = "browser";
        public string DeviceName { get; set; } = "Browser";
    }
}
