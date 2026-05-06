namespace ClinicIntelligente.Core.Application.Mappings;

using AutoMapper;
using ClinicIntelligente.Core.Application.DTOs.Patient;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;
using System.Data;

public class PatientProfile : Profile
{
    public PatientProfile()
    {
        CreateMap<Patient, PatientDto>()
            .ForMember(d => d.FullName,                  o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.Address,                   o => o.MapFrom(s => s.Address))
            .ForMember(d => d.EmergencyContactName,      o => o.MapFrom(s => s.EmergencyContactName))
            .ForMember(d => d.EmergencyContactRelation,  o => o.MapFrom(s => s.EmergencyContactRelation))
            .ForMember(d => d.EmergencyContactPhone,     o => o.MapFrom(s => s.EmergencyContactPhone));

        CreateMap<CreatePatientDto, Patient>()
            .ForMember(d => d.Role, o => o.MapFrom(_ => Role.Patient))
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.PasswordHash, o => o.Ignore());

        CreateMap<UpdatePatientDto, Patient>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Email, o => o.Ignore())
            .ForMember(d => d.Cin, o => o.Ignore());
    }
}