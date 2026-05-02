namespace HelpDeskSystem.API.Services;

public interface IRefreshTokenService
{
    Task<(string token, DateTime expiresAtUtc)> CreateAsync(
        int userId,
        string deviceId,
        string deviceName,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default);
    Task<RefreshTokenRotateResult> RotateAsync(
        string refreshToken,
        string deviceId,
        string deviceName,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(string refreshToken, int? userId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserSessionInfo>> GetSessionsAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> RevokeSessionFamilyAsync(int userId, string familyId, CancellationToken cancellationToken = default);
    Task<int> RevokeAllSessionsAsync(int userId, CancellationToken cancellationToken = default);
}
