namespace ClinicIntelligente.Core.Application.Interfaces.Services;

using ClinicIntelligente.Core.Application.DTOs.Doctor;

public interface IDoctorService
{
    Task<IReadOnlyList<DoctorDto>> GetAllAsync();
    Task<DoctorDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<DoctorDto>> GetBySpecialtyAsync(int specialtyId);
    Task<DoctorDto> CreateAsync(CreateDoctorDto dto);
    Task UpdateAsync(int id, UpdateDoctorDto dto);
    Task DeleteAsync(int id);
}