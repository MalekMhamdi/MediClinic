namespace ClinicIntelligente.Core.Application.Interfaces.Services;

using ClinicIntelligente.Core.Application.DTOs.Specialty;

public interface ISpecialtyService
{
    Task<IReadOnlyList<SpecialtyDto>> GetAllAsync();
    Task<SpecialtyDto?> GetByIdAsync(int id);
    Task<SpecialtyDto> CreateAsync(CreateSpecialtyDto dto);
    Task UpdateAsync(int id, CreateSpecialtyDto dto);
    Task DeleteAsync(int id);
}