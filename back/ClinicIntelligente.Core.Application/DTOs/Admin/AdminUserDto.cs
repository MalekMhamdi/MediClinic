namespace ClinicIntelligente.Core.Application.DTOs.Admin;

using System.ComponentModel.DataAnnotations;

public record AdminUserDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateAdminUserDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password,
    [Required] string Role
);

public record UpdateAdminUserDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required][EmailAddress] string Email
);
