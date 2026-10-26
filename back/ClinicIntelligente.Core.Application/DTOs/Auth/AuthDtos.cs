using System.ComponentModel.DataAnnotations;

namespace ClinicIntelligente.Core.Application.DTOs.Auth;

public record LoginDto(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record RegisterPatientDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password,
    [Required] string Cin,
    [Required] DateTime DateOfBirth,
    string? BloodGroup,
    string? Phone,
    string? Address,
    string? EmergencyContactName,
    string? EmergencyContactRelation,
    string? EmergencyContactPhone
);

public record UpdateProfileDto(
    [Required] string FirstName,
    [Required] string LastName,
    [EmailAddress] string? Email,
    string? Phone,
    string? BloodGroup,
    string? Address,
    string? EmergencyContactName,
    string? EmergencyContactRelation,
    string? EmergencyContactPhone
);

public record ChangePasswordDto(
    [Required] string CurrentPassword,
    [Required][MinLength(8)] string NewPassword
);

public record AuthResponseDto(
    int UserId,
    string FullName,
    string Email,
    string Role,
    string Token,
    DateTime ExpiresAt
);