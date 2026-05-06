using System.ComponentModel.DataAnnotations;

namespace ClinicIntelligente.Core.Application.DTOs.Doctor;

public record DoctorDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    int SpecialtyId,
    string SpecialtyName,
    string? Phone,
    string? LicenseNumber,
    string? ImageUrl,
    bool IsActive
);

public record CreateDoctorDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password,
    [Required] int SpecialtyId,
               string? Phone,
               string? LicenseNumber,
               string? OfficeLocation
);

public record UpdateDoctorDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required] int SpecialtyId,
               string? Phone,
               string? LicenseNumber
               
);