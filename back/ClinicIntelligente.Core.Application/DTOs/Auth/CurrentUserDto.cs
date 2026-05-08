namespace ClinicIntelligente.Core.Application.DTOs.Auth;

public record CurrentUserDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive
);