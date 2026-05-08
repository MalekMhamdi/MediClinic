namespace ClinicIntelligente.Core.Application.Interfaces.Services;

using ClinicIntelligente.Core.Application.DTOs.LabOrder;

public interface ILabResultService
{
    Task<LabResultDto?> GetByLabOrderIdAsync(int labOrderId);
    Task<IReadOnlyList<LabResultDto>> GetByTechnicienIdAsync(int technicienId);
    Task<LabResultDto> CreateAsync(CreateLabResultDto dto);
    Task<IReadOnlyList<DoctorLabResultDto>> GetByDoctorIdAsync(int doctorId);
    Task SendDoctorCommentAsync(int labResultId, int doctorId, string comment);
    Task DeleteAsync(int id);
}