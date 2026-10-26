// ClinicIntelligente.Core.Application/Services/AuthService.cs
namespace ClinicIntelligente.Core.Application.Services;

using ClinicIntelligente.Core.Application.DTOs.Auth;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;

public class AuthService(
    IUserRepository userRepo,
    IPatientRepository patientRepo,
    IDoctorRepository doctorRepo,
    ILabTechRepository labTechRepo,
    ISpecialtyRepository specialtyRepo,
    ITokenService tokenService
) : IAuthService
{
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await userRepo.GetByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Email ou mot de passe incorrect.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email ou mot de passe incorrect.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Compte désactivé. Contactez l'administration.");

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientDto dto)
    {
        if (await userRepo.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' déjà utilisé.");

        if (await patientRepo.ExistsByCinAsync(dto.Cin))
            throw new InvalidOperationException($"CIN '{dto.Cin}' déjà enregistré.");

        var patient = new Patient
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Cin = dto.Cin,
            DateOfBirth = dto.DateOfBirth,
            BloodGroup = dto.BloodGroup,
            Phone = dto.Phone,
            Address = dto.Address,
            EmergencyContactName     = dto.EmergencyContactName,
            EmergencyContactRelation = dto.EmergencyContactRelation,
            EmergencyContactPhone    = dto.EmergencyContactPhone,
            Role = Role.Patient,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await patientRepo.AddAsync(patient);
        return BuildResponse(patient);
    }

    public async Task<AuthResponseDto> RegisterDoctorAsync(RegisterDoctorDto dto)
    {
        if (await userRepo.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' déjà utilisé.");

        // Validate specialty exists
        var specialty = await specialtyRepo.GetByIdAsync(dto.SpecialtyId)
            ?? throw new KeyNotFoundException($"Specialty {dto.SpecialtyId} not found.");

        var doctor = new Doctor
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            SpecialtyId = dto.SpecialtyId,
            Phone = dto.Phone,
            LicenseNumber = dto.LicenseNumber,
            Role = Role.Doctor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await doctorRepo.AddAsync(doctor);
        return BuildResponse(doctor);
    }

    public async Task<AuthResponseDto> RegisterLabTechAsync(RegisterLabTechDto dto)
    {
        if (await userRepo.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' déjà utilisé.");

        var labTech = new LabTechnicien
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = dto.Phone,
            Role = Role.LabTechnicien,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await labTechRepo.AddAsync(labTech);
        return BuildResponse(labTech);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(int userId)
    {
        var user = await userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Utilisateur introuvable.");

        return new CurrentUserDto(
            Id: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            Role: user.Role.ToString(),
            IsActive: user.IsActive
        );
    }

    public async Task UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Utilisateur introuvable.");

        user.FirstName = dto.FirstName;
        user.LastName  = dto.LastName;

        // Update email if provided and different
        if (!string.IsNullOrWhiteSpace(dto.Email) &&
            !dto.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await userRepo.ExistsByEmailAsync(dto.Email))
                throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");
            user.Email = dto.Email.Trim().ToLower();
        }

        user.LastModifiedAt = DateTime.UtcNow;

        switch (user)
        {
            case Patient p:
                p.Phone = dto.Phone;
                p.BloodGroup = dto.BloodGroup;
                p.Address = dto.Address;
                p.EmergencyContactName = dto.EmergencyContactName;
                p.EmergencyContactRelation = dto.EmergencyContactRelation;
                p.EmergencyContactPhone = dto.EmergencyContactPhone;
                break;
            case Doctor d:
                d.Phone = dto.Phone;
                break;
            case LabTechnicien lt:
                lt.Phone = dto.Phone;
                break;
        }

        await userRepo.UpdateAsync(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Utilisateur introuvable.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Mot de passe actuel incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.LastModifiedAt = DateTime.UtcNow;
        await userRepo.UpdateAsync(user);
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var (token, expiresAt) = tokenService.GenerateToken(user);
        return new AuthResponseDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            token,
            expiresAt
        );
    }
}