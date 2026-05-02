using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.API.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);
    private readonly HelpDeskDbContext _context;
    private readonly IAuditService _auditService;

    public RefreshTokenService(HelpDeskDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<(string token, DateTime expiresAtUtc)> CreateAsync(
        int userId,
        string deviceId,
        string deviceName,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        var token = GenerateTokenValue();
        var tokenHash = HashToken(token);
        var expiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);
        var familyId = Guid.NewGuid().ToString("N");
        var normalizedDeviceId = NormalizeDeviceId(deviceId);
        var normalizedDeviceName = NormalizeDeviceName(deviceName);

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            FamilyId = familyId,
            DeviceId = normalizedDeviceId,
            DeviceName = normalizedDeviceName,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAt,
            LastUsedAtUtc = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            CreatedByUserAgent = userAgent
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            userId,
            "SESSION_CREATED",
            "RefreshTokenFamily",
            familyId,
            newValues: JsonSerializer.Serialize(new
            {
                DeviceId = normalizedDeviceId,
                DeviceName = normalizedDeviceName,
                ExpiresAtUtc = expiresAt
            }),
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        return (token, expiresAt);
    }

    public async Task<RefreshTokenRotateResult> RotateAsync(
        string refreshToken,
        string deviceId,
        string deviceName,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var existing = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existing == null)
        {
            await _auditService.LogAsync(
                null,
                "SESSION_REFRESH_INVALID_TOKEN",
                "RefreshToken",
                tokenHash,
                ipAddress: ipAddress,
                cancellationToken: cancellationToken);
            return new RefreshTokenRotateResult { Status = RefreshTokenRotateStatus.Invalid };
        }

        if (existing.RevokedAtUtc.HasValue || existing.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await InvalidateFamilyAsync(existing.UserId, existing.FamilyId, "Refresh token reuse detected", cancellationToken);
            await _auditService.LogAsync(
                existing.UserId,
                "SESSION_REFRESH_REUSE_DETECTED",
                "RefreshTokenFamily",
                existing.FamilyId,
                newValues: JsonSerializer.Serialize(new
                {
                    Reason = "Refresh token reuse detected",
                    ExistingTokenId = existing.Id
                }),
                ipAddress: ipAddress,
                cancellationToken: cancellationToken);
            return new RefreshTokenRotateResult { Status = RefreshTokenRotateStatus.ReuseDetected };
        }

        var normalizedDeviceId = NormalizeDeviceId(deviceId);
        if (!string.Equals(existing.DeviceId, normalizedDeviceId, StringComparison.Ordinal))
        {
            await InvalidateFamilyAsync(existing.UserId, existing.FamilyId, "Device mismatch during refresh", cancellationToken);
            await _auditService.LogAsync(
                existing.UserId,
                "SESSION_REFRESH_DEVICE_MISMATCH",
                "RefreshTokenFamily",
                existing.FamilyId,
                oldValues: JsonSerializer.Serialize(new { DeviceId = existing.DeviceId }),
                newValues: JsonSerializer.Serialize(new { DeviceId = normalizedDeviceId }),
                ipAddress: ipAddress,
                cancellationToken: cancellationToken);
            return new RefreshTokenRotateResult { Status = RefreshTokenRotateStatus.ReuseDetected };
        }

        var newToken = GenerateTokenValue();
        var newTokenHash = HashToken(newToken);
        var newExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);
        var normalizedDeviceName = string.IsNullOrWhiteSpace(deviceName) ? existing.DeviceName : NormalizeDeviceName(deviceName);

        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.RevocationReason = "Rotated";
        existing.ReplacedByTokenHash = newTokenHash;
        existing.LastUsedAtUtc = DateTime.UtcNow;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            FamilyId = existing.FamilyId,
            DeviceId = existing.DeviceId,
            DeviceName = normalizedDeviceName,
            TokenHash = newTokenHash,
            ExpiresAtUtc = newExpiresAt,
            LastUsedAtUtc = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            CreatedByUserAgent = userAgent
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            existing.UserId,
            "SESSION_REFRESH_ROTATED",
            "RefreshTokenFamily",
            existing.FamilyId,
            oldValues: JsonSerializer.Serialize(new
            {
                PreviousTokenId = existing.Id,
                PreviousExpiresAtUtc = existing.ExpiresAtUtc
            }),
            newValues: JsonSerializer.Serialize(new
            {
                NewTokenExpiresAtUtc = newExpiresAt,
                DeviceName = normalizedDeviceName
            }),
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        return new RefreshTokenRotateResult
        {
            Status = RefreshTokenRotateStatus.Success,
            UserId = existing.UserId,
            NewRefreshToken = newToken,
            NewRefreshTokenExpiresAtUtc = newExpiresAt
        };
    }

    public async Task<bool> RevokeAsync(string refreshToken, int? userId = null, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var existing = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existing == null || existing.RevokedAtUtc.HasValue)
        {
            return false;
        }

        if (userId.HasValue && existing.UserId != userId.Value)
        {
            return false;
        }

        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.RevocationReason = "User revoked token";
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            userId ?? existing.UserId,
            "SESSION_REVOKED",
            "RefreshTokenFamily",
            existing.FamilyId,
            newValues: JsonSerializer.Serialize(new
            {
                TokenId = existing.Id,
                Reason = existing.RevocationReason
            }),
            cancellationToken: cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<UserSessionInfo>> GetSessionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sessions = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .GroupBy(rt => rt.FamilyId)
            .Select(g => new UserSessionInfo
            {
                FamilyId = g.Key,
                DeviceId = g.OrderByDescending(x => x.CreatedAtUtc).Select(x => x.DeviceId).FirstOrDefault() ?? string.Empty,
                DeviceName = g.OrderByDescending(x => x.CreatedAtUtc).Select(x => x.DeviceName).FirstOrDefault() ?? string.Empty,
                CreatedAtUtc = g.Min(x => x.CreatedAtUtc),
                LastUsedAtUtc = g.Max(x => x.LastUsedAtUtc),
                ExpiresAtUtc = g.Max(x => x.ExpiresAtUtc),
                IsActive = g.Any(x => x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            })
            .OrderByDescending(x => x.LastUsedAtUtc)
            .ToListAsync(cancellationToken);

        return sessions;
    }

    public async Task<bool> RevokeSessionFamilyAsync(int userId, string familyId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.FamilyId == familyId && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAtUtc = now;
            token.RevocationReason = "Session revoked";
            token.UpdatedAtUtc = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            userId,
            "SESSION_FAMILY_REVOKED",
            "RefreshTokenFamily",
            familyId,
            newValues: JsonSerializer.Serialize(new
            {
                RevokedTokenCount = tokens.Count
            }),
            cancellationToken: cancellationToken);
        return true;
    }

    public async Task<int> RevokeAllSessionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAtUtc = now;
            token.RevocationReason = "All sessions revoked";
            token.UpdatedAtUtc = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            userId,
            "ALL_SESSIONS_REVOKED",
            "RefreshTokenUser",
            userId.ToString(),
            newValues: JsonSerializer.Serialize(new
            {
                RevokedTokenCount = tokens.Count
            }),
            cancellationToken: cancellationToken);
        return tokens.Count;
    }

    private async Task InvalidateFamilyAsync(int userId, string familyId, string reason, CancellationToken cancellationToken)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.FamilyId == familyId && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAtUtc = now;
            token.RevocationReason = reason;
            token.UpdatedAtUtc = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            userId,
            "SESSION_FAMILY_INVALIDATED",
            "RefreshTokenFamily",
            familyId,
            newValues: JsonSerializer.Serialize(new
            {
                Reason = reason,
                RevokedTokenCount = tokens.Count
            }),
            cancellationToken: cancellationToken);
    }

    private static string NormalizeDeviceId(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return "unknown-device";

        return deviceId.Trim().ToLowerInvariant();
    }

    private static string NormalizeDeviceName(string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
            return "Unknown Device";

        return deviceName.Trim();
    }

    private static string GenerateTokenValue()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
