namespace ClinicIntelligente.Core.Application.Mappings;

using AutoMapper;
using ClinicIntelligente.Core.Application.DTOs.Doctor;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;
using System.Data;
using System.Numerics;

public class DoctorProfile : Profile
{
    public DoctorProfile()
    {
        CreateMap<Doctor, DoctorDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.SpecialtyName, o => o.MapFrom(s => s.Specialty != null ? s.Specialty.Name : ""));

        CreateMap<CreateDoctorDto, Doctor>()
            .ForMember(d => d.Role, o => o.MapFrom(_ => Role.Doctor))
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.PasswordHash, o => o.Ignore());

        CreateMap<UpdateDoctorDto, Doctor>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Email, o => o.Ignore());
    }
}