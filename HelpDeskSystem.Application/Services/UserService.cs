using HelpDeskSystem.Application.DTOs.Users;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Application.Services;

public class UserService : IUserService
{
    private readonly HelpDeskDbContext _context;
    private readonly IMfaService _mfaService;

    public UserService(HelpDeskDbContext context, IMfaService mfaService)
    {
        _context = context;
        _mfaService = mfaService;
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        return user == null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetUsersByTenantAsync(int tenantId)
    {
        var users = await _context.Users
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();

        return users.Select(MapToDto);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            TenantId = dto.TenantId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(user.Id) ?? throw new Exception("User creation failed");
    }

    public async Task UpdateUserAsync(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null || !user.IsActive) return;

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.IsActive = dto.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return;

        user.IsActive = false;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task AssignRoleToUserAsync(int userId, int roleId)
    {
        if (!await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId))
        {
            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ValidatePasswordAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        return user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public async Task<string> SetMfaSecretAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive)
                   ?? throw new InvalidOperationException("User not found.");

        user.MfaSecret = _mfaService.GenerateSharedSecret();
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return user.MfaSecret;
    }

    public async Task<bool> EnableMfaAsync(int userId, string code)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null || string.IsNullOrWhiteSpace(user.MfaSecret))
            return false;

        if (!_mfaService.VerifyCode(user.MfaSecret, code))
            return false;

        user.IsMfaEnabled = true;
        user.MfaEnabledAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task DisableMfaAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null)
            return;

        user.IsMfaEnabled = false;
        user.MfaSecret = string.Empty;
        user.MfaEnabledAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> VerifyMfaCodeAsync(int userId, string code)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null || !user.IsMfaEnabled || string.IsNullOrWhiteSpace(user.MfaSecret))
            return false;

        return _mfaService.VerifyCode(user.MfaSecret, code);
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            IsMfaEnabled = user.IsMfaEnabled,
            TenantId = user.TenantId,
            Roles = user.UserRoles.Select(ur => ur.Role?.Name ?? string.Empty).ToList()
        };
    }
}
