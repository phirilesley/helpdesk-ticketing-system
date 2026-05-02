using HelpDeskSystem.API.Security;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using HelpDeskSystem.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml;

namespace HelpDeskSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/identity")]
public class AdminIdentityController : ControllerBase
{
    private readonly HelpDeskDbContext _context;

    public AdminIdentityController(HelpDeskDbContext context)
    {
        _context = context;
    }

    [HttpGet("providers")]
    public async Task<ActionResult<IEnumerable<IdentityProviderConfig>>> GetProviders([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var providers = await _context.IdentityProviderConfigs
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(providers);
    }

    [HttpPost("providers")]
    public async Task<ActionResult<IdentityProviderConfig>> UpsertProvider(
        [FromBody] UpsertIdentityProviderRequest request,
        [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        IdentityProviderConfig entity;
        if (request.Id.HasValue)
        {
            entity = await _context.IdentityProviderConfigs
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new IdentityProviderConfig { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new IdentityProviderConfig { TenantId = resolvedTenantId.Value };
            _context.IdentityProviderConfigs.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Protocol = request.Protocol;
        entity.Issuer = request.Issuer.Trim();
        entity.AuthorityOrMetadataUrl = request.AuthorityOrMetadataUrl.Trim();
        entity.ClientId = request.ClientId.Trim();
        entity.ClientSecret = request.ClientSecret.Trim();
        entity.Audience = request.Audience.Trim();
        entity.EnforceSso = request.EnforceSso;
        entity.EnforceStrictIssuer = request.EnforceStrictIssuer;
        entity.AllowedRedirectUrisJson = request.AllowedRedirectUrisJson.Trim();
        entity.OidcRequirePkce = request.OidcRequirePkce;
        entity.SamlValidateSignature = request.SamlValidateSignature;
        entity.SamlAllowIdpInitiated = request.SamlAllowIdpInitiated;
        entity.SamlAllowedCertificateThumbprints = request.SamlAllowedCertificateThumbprints.Trim();
        entity.SamlSpEntityId = request.SamlSpEntityId.Trim();
        entity.SamlAcsUrl = request.SamlAcsUrl.Trim();
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.IdentityProviderConfigs.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    [HttpPost("providers/{providerId:int}/metadata")]
    public async Task<ActionResult<IdentityProviderConfig>> UploadSamlMetadata(
        int providerId,
        [FromBody] UploadSamlMetadataRequest request,
        [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var provider = await _context.IdentityProviderConfigs
            .FirstOrDefaultAsync(x => x.Id == providerId && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted);
        if (provider == null)
            return NotFound();

        if (provider.Protocol != IdentityProtocol.Saml)
            return BadRequest(new { error = "Metadata upload only applies to SAML providers." });

        if (string.IsNullOrWhiteSpace(request.MetadataXml))
            return BadRequest(new { error = "metadataXml is required." });

        try
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(request.MetadataXml);
            provider.SamlMetadataXml = request.MetadataXml.Trim();

            var certNodes = doc.SelectNodes("//*[local-name()='X509Certificate']");
            if (certNodes != null && certNodes.Count > 0)
            {
                var thumbprints = new List<string>();
                foreach (XmlNode node in certNodes)
                {
                    var raw = node.InnerText?.Trim();
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    try
                    {
                        var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(Convert.FromBase64String(raw));
                        thumbprints.Add(cert.Thumbprint?.Replace(" ", string.Empty, StringComparison.Ordinal) ?? string.Empty);
                    }
                    catch
                    {
                        // Ignore bad cert nodes; strict validation at runtime handles failures.
                    }
                }

                provider.SamlAllowedCertificateThumbprints = string.Join(',', thumbprints.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
            }

            provider.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok(provider);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Invalid metadata XML: {ex.Message}" });
        }
    }

    [HttpGet("abac")]
    public async Task<ActionResult<IEnumerable<AbacPolicyRule>>> GetAbacRules([FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        var rules = await _context.AbacPolicyRules
            .Where(x => x.TenantId == resolvedTenantId.Value && !x.IsDeleted)
            .OrderBy(x => x.Priority)
            .ToListAsync(HttpContext.RequestAborted);
        return Ok(rules);
    }

    [HttpPost("abac")]
    public async Task<ActionResult<AbacPolicyRule>> UpsertAbacRule([FromBody] UpsertAbacPolicyRequest request, [FromQuery] int? tenantId = null)
    {
        var resolvedTenantId = ResolveTenantId(tenantId);
        if (!resolvedTenantId.HasValue) return Forbid();

        AbacPolicyRule entity;
        if (request.Id.HasValue)
        {
            entity = await _context.AbacPolicyRules
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == resolvedTenantId.Value && !x.IsDeleted, HttpContext.RequestAborted)
                ?? new AbacPolicyRule { TenantId = resolvedTenantId.Value };
        }
        else
        {
            entity = new AbacPolicyRule { TenantId = resolvedTenantId.Value };
            _context.AbacPolicyRules.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Resource = request.Resource.Trim();
        entity.Action = request.Action.Trim();
        entity.ConditionJson = request.ConditionJson.Trim();
        entity.Effect = request.Effect;
        entity.Priority = request.Priority;
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.Id == 0)
            _context.AbacPolicyRules.Add(entity);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(entity);
    }

    private int? ResolveTenantId(int? tenantId)
    {
        if (User.IsInRole("SuperAdmin"))
            return tenantId ?? User.GetTenantId();
        return User.GetTenantId();
    }
}

public class UpsertIdentityProviderRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IdentityProtocol Protocol { get; set; } = IdentityProtocol.Oidc;
    public string Issuer { get; set; } = string.Empty;
    public string AuthorityOrMetadataUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool EnforceSso { get; set; }
    public bool EnforceStrictIssuer { get; set; } = true;
    public string AllowedRedirectUrisJson { get; set; } = "[]";
    public bool OidcRequirePkce { get; set; } = true;
    public bool SamlValidateSignature { get; set; } = true;
    public bool SamlAllowIdpInitiated { get; set; }
    public string SamlAllowedCertificateThumbprints { get; set; } = string.Empty;
    public string SamlSpEntityId { get; set; } = string.Empty;
    public string SamlAcsUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class UploadSamlMetadataRequest
{
    public string MetadataXml { get; set; } = string.Empty;
}

public class UpsertAbacPolicyRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ConditionJson { get; set; } = "{}";
    public PolicyEffect Effect { get; set; } = PolicyEffect.Allow;
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
}
