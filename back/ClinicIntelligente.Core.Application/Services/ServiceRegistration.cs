
// Interfaces
using ClinicIntelligente.Core.Application.Interfaces.Services;
// Services (implementations)
using ClinicIntelligente.Core.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using AutoMapper;

#pragma warning disable IDE0130
namespace ClinicIntelligente.Core.Application;
#pragma warning disable IDE0130


public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<ILabTechService, LabTechService>();
        services.AddScoped<ILabResultService, LabResultService>();
        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IDoctorReviewService, DoctorReviewService>();
        services.AddHttpClient<IAiOrientationService, AiOrientationService>();

        return services;
    }
}