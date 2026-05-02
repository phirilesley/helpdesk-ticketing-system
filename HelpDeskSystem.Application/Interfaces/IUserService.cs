using HelpDeskSystem.Application.DTOs.Users;

namespace HelpDeskSystem.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetUsersByTenantAsync(int tenantId);
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task UpdateUserAsync(int id, UpdateUserDto dto);
    Task DeleteUserAsync(int id);
    Task AssignRoleToUserAsync(int userId, int roleId);
    Task<bool> ValidatePasswordAsync(string email, string password);
    Task<string> SetMfaSecretAsync(int userId);
    Task<bool> EnableMfaAsync(int userId, string code);
    Task DisableMfaAsync(int userId);
    Task<bool> VerifyMfaCodeAsync(int userId, string code);
}
