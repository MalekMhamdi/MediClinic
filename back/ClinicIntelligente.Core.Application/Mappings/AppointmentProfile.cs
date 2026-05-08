namespace ClinicIntelligente.Core.Application.Mappings;

using AutoMapper;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;
using ClinicIntelligente.Core.Application.DTOs.Appointment;

public class AppointmentProfile : Profile
{
    public AppointmentProfile()
    {
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(d => d.PatientFullName,
                o => o.MapFrom(s => s.Patient != null ? $"{s.Patient.FirstName} {s.Patient.LastName}" : ""))
            .ForMember(d => d.DoctorFullName,
                o => o.MapFrom(s => s.Doctor != null ? $"{s.Doctor.FirstName} {s.Doctor.LastName}" : ""))
            .ForMember(d => d.DoctorSpecialty,
                o => o.MapFrom(s => s.Doctor != null && s.Doctor.Specialty != null ? s.Doctor.Specialty.Name : ""))
            .ForMember(d => d.StatusLabel,
                o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.HasConsultation,
                o => o.MapFrom(s => s.Consultation != null));

        CreateMap<CreateAppointmentDto, Appointment>()
            .ForMember(d => d.Status, o => o.MapFrom(_ => AppointmentStatus.Pending));
    }
}