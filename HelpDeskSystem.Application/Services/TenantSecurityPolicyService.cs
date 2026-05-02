using System.Net;
using System.Security.Cryptography;
using System.Text;
using HelpDeskSystem.Application.DTOs.Security;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Application.Services;

public class TenantSecurityPolicyService : ITenantSecurityPolicyService
{
    private readonly HelpDeskDbContext _context;

    public TenantSecurityPolicyService(HelpDeskDbContext context)
    {
        _context = context;
    }

    public async Task<TenantSecurityPolicyDto> GetPolicyAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var policy = await _context.TenantSecurityPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (policy == null)
        {
            return new TenantSecurityPolicyDto
            {
                TenantId = tenantId,
                RequireMfaForPrivilegedUsers = false,
                AllowedIpRanges = string.Empty,
                BlockInboundEmailTicketCreation = false,
                HasScimToken = false
            };
        }

        return Map(policy);
    }

    public async Task<TenantSecurityPolicyDto> UpsertPolicyAsync(int tenantId, UpsertTenantSecurityPolicyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TenantSecurityPolicies
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            entity = new TenantSecurityPolicy { TenantId = tenantId };
            _context.TenantSecurityPolicies.Add(entity);
        }

        entity.RequireMfaForPrivilegedUsers = dto.RequireMfaForPrivilegedUsers;
        entity.AllowedIpRanges = dto.AllowedIpRanges.Trim();
        entity.BlockInboundEmailTicketCreation = dto.BlockInboundEmailTicketCreation;
        if (!string.IsNullOrWhiteSpace(dto.ScimBearerToken))
        {
            entity.ScimBearerTokenHash = HashToken(dto.ScimBearerToken);
        }
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> IsIpAllowedAsync(int tenantId, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip))
            return false;

        var ranges = await _context.TenantSecurityPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => x.AllowedIpRanges)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(ranges))
            return true;

        var entries = ranges.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var entry in entries)
        {
            if (IsIpInCidr(ip, entry))
                return true;
        }
        return false;
    }

    public async Task<bool> RequiresMfaForUserAsync(int tenantId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var require = await _context.TenantSecurityPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => x.RequireMfaForPrivilegedUsers)
            .FirstOrDefaultAsync(cancellationToken);

        if (!require)
            return false;

        var privileged = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin", "SuperAdmin", "Agent" };
        return roles.Any(r => privileged.Contains(r));
    }

    public async Task<bool> ValidateScimTokenAsync(int tenantId, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var hash = await _context.TenantSecurityPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => x.ScimBearerTokenHash)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(hash))
            return false;

        return string.Equals(hash, HashToken(token), StringComparison.Ordinal);
    }

    public async Task<bool> IsInboundEmailBlockedAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantSecurityPolicies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => x.BlockInboundEmailTicketCreation)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsIpInCidr(IPAddress ip, string cidr)
    {
        var parts = cidr.Split('/', 2, StringSplitOptions.TrimEntries);
        if (!IPAddress.TryParse(parts[0], out var network))
            return false;

        if (parts.Length == 1)
            return ip.Equals(network);

        if (!int.TryParse(parts[1], out var prefixLength))
            return false;

        var ipBytes = ip.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();
        if (ipBytes.Length != networkBytes.Length)
            return false;

        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (ipBytes[i] != networkBytes[i])
                return false;
        }

        if (remainingBits > 0)
        {
            var mask = (byte)(0xFF << (8 - remainingBits));
            if ((ipBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
                return false;
        }

        return true;
    }

    private static string HashToken(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(bytes);
    }

    private static TenantSecurityPolicyDto Map(TenantSecurityPolicy entity)
    {
        return new TenantSecurityPolicyDto
        {
            TenantId = entity.TenantId,
            RequireMfaForPrivilegedUsers = entity.RequireMfaForPrivilegedUsers,
            AllowedIpRanges = entity.AllowedIpRanges,
            BlockInboundEmailTicketCreation = entity.BlockInboundEmailTicketCreation,
            HasScimToken = !string.IsNullOrWhiteSpace(entity.ScimBearerTokenHash)
        };
    }
}
