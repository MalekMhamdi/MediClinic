using System.ComponentModel.DataAnnotations;

namespace ClinicIntelligente.Core.Application.DTOs.Patient;

public record PatientDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Cin,
    DateTime DateOfBirth,
    string? BloodGroup,
    string? Phone,
    string? ImageUrl,
    string? Address,
    string? EmergencyContactName,
    string? EmergencyContactRelation,
    string? EmergencyContactPhone,
    bool IsActive
);

public record CreatePatientDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password,
    [Required] string Cin,
               DateTime DateOfBirth,
               string? BloodGroup,
               string? Phone,
               string? Address
);

public record UpdatePatientDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required][EmailAddress] string Email,
    [Required] string Cin,
               string? BloodGroup,
               string? Phone,
               string? Address
);