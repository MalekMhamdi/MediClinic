namespace ClinicIntelligente.Core.Application.Mappings;

using AutoMapper;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;
using ClinicIntelligente.Core.Application.DTOs.Consultation;
using ClinicIntelligente.Core.Application.DTOs.Prescription;
using ClinicIntelligente.Core.Application.DTOs.LabOrder;

public class ConsultationProfile : Profile
{
    public ConsultationProfile()
    {
        CreateMap<Consultation, ConsultationDto>()
            .ForMember(d => d.Prescriptions,
                o => o.MapFrom(s => s.Prescriptions ?? new List<Prescription>()))
            .ForMember(d => d.LabOrders,
                o => o.MapFrom(s => s.LabOrders ?? new List<LabOrder>()));

        CreateMap<CreateConsultationDto, Consultation>();

        CreateMap<Prescription, PrescriptionDto>();
        CreateMap<CreatePrescriptionDto, Prescription>();

        CreateMap<LabOrder, LabOrderDto>()
            .ForMember(d => d.StatusLabel, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Result, o => o.MapFrom(s => s.LabResult));

        CreateMap<CreateLabOrderDto, LabOrder>()
            .ForMember(d => d.Status, o => o.MapFrom(_ => LabStatus.Pending));

        CreateMap<LabResult, LabResultDto>()
            .ForMember(d => d.TechnicienName,
                o => o.MapFrom(s => s.Technicien != null
                    ? $"{s.Technicien.FirstName} {s.Technicien.LastName}" : ""))
            .ForMember(d => d.StatusLabel, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<CreateLabResultDto, LabResult>()
            .ForMember(d => d.Status, o => o.MapFrom(_ => LabStatus.Done));

        CreateMap<Specialty, ClinicIntelligente.Core.Application.DTOs.Specialty.SpecialtyDto>();
        CreateMap<ClinicIntelligente.Core.Application.DTOs.Specialty.CreateSpecialtyDto, Specialty>();
    }
}