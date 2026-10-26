using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;
using ClinicIntelligente.Infrastructure.Persistence.Repositories;
using ClinicIntelligente.Infrastructure.Persistence.Services;

namespace ClinicIntelligente.Infrastructure.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ───────────── DB CONTEXT ─────────────
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(
                    typeof(ApplicationDbContext).Assembly.FullName)));

        // ───────────── TOKEN (stateless) ─────────────
        services.AddTransient<ITokenService, TokenService>();

        // ───────────── GENERIC REPOSITORY ─────────────
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // ───────────── ALL REPOSITORIES ─────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<ILabOrderRepository, LabOrderRepository>();
        services.AddScoped<ILabResultRepository, LabResultRepository>();
        services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ILabTechRepository, LabTechRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IDoctorReviewRepository, DoctorReviewRepository>();

        // ───────────── EMAIL ─────────────
        services.AddSingleton<IEmailService, EmailService>();

        return services;
    }
}