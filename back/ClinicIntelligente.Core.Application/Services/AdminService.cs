// ClinicIntelligente.Core.Application/Services/AdminService.cs
namespace ClinicIntelligente.Core.Application.Services;

using ClinicIntelligente.Core.Application.DTOs.Admin;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;

public class AdminService(
    IUserRepository userRepository,
    IAppointmentRepository appointmentRepository,
    IConsultationRepository consultationRepository,
    ILabOrderRepository labOrderRepository
) : IAdminService
{
    public async Task<AdminStatsDto> GetStatsAsync()
    {
        var users        = await userRepository.GetAllAsync();
        var appointments = await appointmentRepository.GetAllWithDetailsAsync();
        var consultations= await consultationRepository.GetAllAsync();
        var labOrders    = await labOrderRepository.GetAllAsync();

        var total     = appointments.Count;
        var cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled);
        var cancelRate= total > 0 ? Math.Round((double)cancelled / total * 100, 1) : 0;

        var byMonth = appointments
            .GroupBy(a => a.DateTime.ToString("MMM yyyy"))
            .Select(g => new MonthlyStatDto(g.Key, g.Count()))
            .OrderBy(m => m.Month)
            .ToList();

        var bySpecialty = appointments
            .Where(a => a.Doctor?.Specialty != null)
            .GroupBy(a => a.Doctor!.Specialty!.Name)
            .Select(g => new SpecialtyStatDto(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .ToList();

        var byDoctor = appointments
            .Where(a => a.Doctor != null)
            .GroupBy(a => a.Doctor!)
            .Select(g => new DoctorStatDto(
                Doctor:        $"{g.Key.FirstName} {g.Key.LastName}",
                Specialty:     g.Key.Specialty?.Name ?? "—",
                Appointments:  g.Count(),
                Consultations: g.Count(a => a.Consultation != null)
            ))
            .OrderByDescending(d => d.Appointments)
            .ToList();

        return new AdminStatsDto(
            TotalPatients:        users.Count(u => u.Role == Role.Patient),
            TotalDoctors:         users.Count(u => u.Role == Role.Doctor),
            TotalLabTechs:        users.Count(u => u.Role == Role.LabTechnicien),
            TotalAppointments:    total,
            PendingAppointments:  appointments.Count(a => a.Status == AppointmentStatus.Pending),
            CancelledAppointments: cancelled,
            CancellationRate:     cancelRate,
            TotalConsultations:   consultations.Count,
            TotalLabOrders:       labOrders.Count,
            AppointmentsByMonth:  byMonth,
            AppointmentsBySpecialty: bySpecialty,
            AppointmentsByDoctor: byDoctor
        );
    }

    public async Task<IReadOnlyList<AdminAppointmentDto>> GetAllAppointmentsAsync(
        string? status = null, string? specialty = null)
    {
        var appointments = await appointmentRepository.GetAllWithDetailsAsync();

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var s))
            appointments = appointments.Where(a => a.Status == s).ToList();

        if (!string.IsNullOrEmpty(specialty))
            appointments = appointments
                .Where(a => a.Doctor?.Specialty?.Name
                    .Contains(specialty, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

        return appointments
            .OrderByDescending(a => a.DateTime)
            .Select(a => new AdminAppointmentDto(
                Id:              a.Id,
                PatientName:     a.Patient is not null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : "—",
                DoctorName:      a.Doctor  is not null ? $"{a.Doctor.FirstName} {a.Doctor.LastName}" : "—",
                DoctorSpecialty: a.Doctor?.Specialty?.Name ?? "—",
                DateTime:        DateTime.SpecifyKind(a.DateTime, DateTimeKind.Utc),
                Status:          a.Status.ToString(),
                StatusLabel:     a.Status.ToString(),
                HasConsultation: a.Consultation is not null,
                Reason:          a.Reason
            ))
            .ToList();
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync()
    {
        var users = await userRepository.GetAllAsync();

        return users
            .Select(u => new AdminUserDto(
                Id: u.Id,
                FullName: u.FullName,
                Email: u.Email,
                Role: u.Role.ToString(),   // enum → string for the DTO
                IsActive: u.IsActive,
                CreatedAt: u.CreatedAt
            ))
            .ToList();
    }

    public async Task ToggleUserActiveAsync(int userId)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (user.Role == Role.Admin)
            throw new InvalidOperationException("Cannot deactivate an admin account.");

        user.IsActive = !user.IsActive;
        await userRepository.UpdateAsync(user);
    }
    public async Task ChangeUserRoleAsync(int userId, string newRole)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (user.Role == Role.Admin)
            throw new InvalidOperationException("Cannot change admin role.");

        if (!Enum.TryParse<Role>(newRole, ignoreCase: true, out var role))
            throw new ArgumentException($"Invalid role '{newRole}'.");

        user.Role = role;
        user.LastModifiedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user);
    }

    public async Task<AdminUserDto> CreateUserAsync(CreateAdminUserDto dto)
    {
        if (await userRepository.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' already in use.");

        if (!Enum.TryParse<Role>(dto.Role, ignoreCase: true, out var role))
            throw new ArgumentException($"Invalid role '{dto.Role}'.");

        var user = new User
        {
            FirstName    = dto.FirstName,
            LastName     = dto.LastName,
            Email        = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role         = role,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        await userRepository.AddAsync(user);
        return new AdminUserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt);
    }

    public async Task UpdateUserAsync(int userId, UpdateAdminUserDto dto)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.FirstName       = dto.FirstName;
        user.LastName        = dto.LastName;
        user.Email           = dto.Email;
        user.LastModifiedAt  = DateTime.UtcNow;
        await userRepository.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(int userId)
    {
        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (user.Role == Role.Admin)
            throw new InvalidOperationException("Cannot delete an admin account.");

        await userRepository.DeleteAsync(user);
    }
}