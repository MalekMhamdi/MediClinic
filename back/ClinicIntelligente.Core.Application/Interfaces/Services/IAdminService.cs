namespace ClinicIntelligente.Core.Application.Interfaces.Services;

using ClinicIntelligente.Core.Application.DTOs.Admin;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync();
    Task<IReadOnlyList<AdminAppointmentDto>> GetAllAppointmentsAsync(string? status = null, string? specialty = null);
    Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync();
    Task ToggleUserActiveAsync(int userId);
    Task ChangeUserRoleAsync(int userId, string newRole);
    Task<AdminUserDto> CreateUserAsync(CreateAdminUserDto dto);
    Task UpdateUserAsync(int userId, UpdateAdminUserDto dto);
    Task DeleteUserAsync(int userId);
}