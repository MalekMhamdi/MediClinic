

using BCrypt.Net;
using AutoMapper;
using ClinicIntelligente.Core.Application.DTOs.Patient;
using ClinicIntelligente.Core.Application.DTOs.MedicalRecord;
using ClinicIntelligente.Core.Application.DTOs.Appointment;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;

namespace ClinicIntelligente.Core.Application.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _repo;
    private readonly IMapper _mapper;

    public PatientService(IPatientRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PatientDto>> GetAllAsync()
        => _mapper.Map<IReadOnlyList<PatientDto>>(await _repo.GetAllAsync());

    public async Task<PatientDto?> GetByIdAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        return p is null ? null : _mapper.Map<PatientDto>(p);
    }

    public async Task<PatientDto?> GetByCinAsync(string cin)
    {
        var p = await _repo.GetByCinAsync(cin);
        return p is null ? null : _mapper.Map<PatientDto>(p);
    }

    public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
    {
        if (await _repo.ExistsByCinAsync(dto.Cin))
            throw new InvalidOperationException($"A patient with CIN '{dto.Cin}' already exists.");

        if (await _repo.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");

        var patient = _mapper.Map<Patient>(dto);
        patient.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        patient.Role = Role.Patient;
        patient.IsActive = true;
        patient.CreatedAt = DateTime.UtcNow;

        await _repo.AddAsync(patient);
        return _mapper.Map<PatientDto>(patient);
    }

    public async Task UpdateAsync(int id, UpdatePatientDto dto)
    {
        var patient = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Patient {id} not found.");

        _mapper.Map(dto, patient);
        patient.LastModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(patient);
    }

    public async Task DeleteAsync(int id)
    {
        var patient = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Patient {id} not found.");

        patient.IsActive = false;
        patient.LastModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(patient);
    }

    public async Task<PatientMedicalRecordDto?> GetMedicalRecordAsync(int id)
    {
        var patient = await _repo.GetWithFullMedicalRecordAsync(id);
        if (patient is null) return null;

        var appointments = (patient.Appointments ?? new List<Domain.Entities.Appointment>())
            .OrderByDescending(a => a.DateTime)
            .Select(a => new MedicalRecordAppointmentDto(
                Id:              a.Id,
                DoctorFullName:  a.Doctor is not null ? $"{a.Doctor.FirstName} {a.Doctor.LastName}" : "—",
                DoctorSpecialty: a.Doctor?.Specialty?.Name ?? "—",
                DateTime:        DateTime.SpecifyKind(a.DateTime, DateTimeKind.Utc),
                Status:          a.Status.ToString(),
                StatusLabel:     a.Status.ToString(),
                Reason:          a.Reason,
                HasConsultation: a.Consultation is not null,
                Consultation:    a.Consultation is null ? null : new MedicalConsultationDto(
                    Id:           a.Consultation.Id,
                    Diagnostic:   a.Consultation.Diagnostic,
                    Observations: a.Consultation.Observations,
                    Treatment:    a.Consultation.Treatment,
                    CreatedAt:    DateTime.SpecifyKind(a.Consultation.CreatedAt, DateTimeKind.Utc),
                    Prescriptions: (a.Consultation.Prescriptions ?? new List<Domain.Entities.Prescription>())
                        .Select(p => new MedicalPrescriptionDto(p.Id, p.Medication, p.Dosage, p.Duration, p.Instructions))
                        .ToList(),
                    LabOrders: (a.Consultation.LabOrders ?? new List<Domain.Entities.LabOrder>())
                        .Select(lo => new MedicalLabOrderDto(
                            Id:           lo.Id,
                            AnalysisType: lo.AnalysisType,
                            Status:       lo.Status.ToString(),
                            StatusLabel:  lo.Status.ToString(),
                            Notes:        lo.Notes,
                            Result:       lo.LabResult is null ? null : new MedicalLabResultDto(
                                Id:             lo.LabResult.Id,
                                TechnicienName: lo.LabResult.Technicien is not null
                                    ? $"{lo.LabResult.Technicien.FirstName} {lo.LabResult.Technicien.LastName}"
                                    : "—",
                                MedicalComment: lo.LabResult.MedicalComment,
                                ResultPdfPath:  lo.LabResult.ResultPdfPath,
                                Status:         lo.LabResult.Status.ToString(),
                                CreatedAt:      DateTime.SpecifyKind(lo.LabResult.CreatedAt, DateTimeKind.Utc)
                            )
                        ))
                        .ToList()
                )
            ))
            .ToList();

        return new PatientMedicalRecordDto(
            patient.Id,
            $"{patient.FirstName} {patient.LastName}",
            patient.Cin,
            patient.DateOfBirth,
            patient.BloodGroup,
            patient.Phone,
            appointments
        );
    }
}