namespace ClinicIntelligente.Core.Application.DTOs.Admin;

using System.ComponentModel.DataAnnotations;

public record ChangeRoleDto(
    [Required] string Role
);