// ClinicIntelligente.Core.Application/Interfaces/Services/IAuthService.cs
using ClinicIntelligente.Core.Application.DTOs.Auth;

namespace ClinicIntelligente.Core.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientDto dto);
    Task<AuthResponseDto> RegisterDoctorAsync(RegisterDoctorDto dto);
    Task<AuthResponseDto> RegisterLabTechAsync(RegisterLabTechDto dto);
    Task<CurrentUserDto> GetCurrentUserAsync(int userId);
    Task UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
}