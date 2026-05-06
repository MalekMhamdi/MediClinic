namespace ClinicIntelligente.Core.Application.DTOs.Auth;

using System.ComponentModel.DataAnnotations;

public record RegisterLabTechDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    string? Phone
);