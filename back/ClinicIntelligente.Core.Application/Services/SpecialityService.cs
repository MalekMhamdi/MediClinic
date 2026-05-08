namespace ClinicIntelligente.Core.Application.Services;

using AutoMapper;
using ClinicIntelligente.Core.Application.DTOs.Specialty;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;

public class SpecialtyService : ISpecialtyService
{
    private readonly ISpecialtyRepository _repo;
    private readonly IMapper _mapper;

    public SpecialtyService(ISpecialtyRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SpecialtyDto>> GetAllAsync()
        => _mapper.Map<IReadOnlyList<SpecialtyDto>>(await _repo.GetAllAsync());

    public async Task<SpecialtyDto?> GetByIdAsync(int id)
    {
        var s = await _repo.GetByIdAsync(id);
        return s is null ? null : _mapper.Map<SpecialtyDto>(s);
    }

    public async Task<SpecialtyDto> CreateAsync(CreateSpecialtyDto dto)
    {
        if (await _repo.ExistsByNameAsync(dto.Name))
            throw new InvalidOperationException($"Specialty '{dto.Name}' already exists.");
        var specialty = _mapper.Map<Domain.Entities.Specialty>(dto);
        await _repo.AddAsync(specialty);
        return _mapper.Map<SpecialtyDto>(specialty);
    }

    public async Task UpdateAsync(int id, CreateSpecialtyDto dto)
    {
        var specialty = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Specialty {id} not found.");
        _mapper.Map(dto, specialty);
        specialty.LastModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(specialty);
    }

    public async Task DeleteAsync(int id)
    {
        var specialty = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Specialty {id} not found.");
        await _repo.DeleteAsync(specialty);
    }
}