

using BCrypt.Net;
using AutoMapper;
using ClinicIntelligente.Core.Application.DTOs.Doctor;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;

namespace ClinicIntelligente.Core.Application.Services;  

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _repo;
    private readonly IMapper _mapper;

    public DoctorService(IDoctorRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<DoctorDto>> GetAllAsync()
        => _mapper.Map<IReadOnlyList<DoctorDto>>(await _repo.GetAllAsync());

    public async Task<DoctorDto?> GetByIdAsync(int id)
    {
        var d = await _repo.GetByIdAsync(id);
        return d is null ? null : _mapper.Map<DoctorDto>(d);
    }

    public async Task<IReadOnlyList<DoctorDto>> GetBySpecialtyAsync(int specialtyId)
        => _mapper.Map<IReadOnlyList<DoctorDto>>(await _repo.GetBySpecialtyAsync(specialtyId));

    public async Task<DoctorDto> CreateAsync(CreateDoctorDto dto)
    {
        if (await _repo.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");

        var doctor = _mapper.Map<Doctor>(dto);
        doctor.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        doctor.Role = Role.Doctor;
        doctor.IsActive = true;
        doctor.CreatedAt = DateTime.UtcNow;

        await _repo.AddAsync(doctor);
        return _mapper.Map<DoctorDto>(doctor);
    }

    public async Task UpdateAsync(int id, UpdateDoctorDto dto)
    {
        var doctor = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Doctor {id} not found.");

        _mapper.Map(dto, doctor);
        doctor.LastModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(doctor);
    }

    public async Task DeleteAsync(int id)
    {
        var doctor = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Doctor {id} not found.");

        doctor.IsActive = false;
        doctor.LastModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(doctor);
    }
}