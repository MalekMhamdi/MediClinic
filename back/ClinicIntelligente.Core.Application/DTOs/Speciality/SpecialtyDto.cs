using System.ComponentModel.DataAnnotations;

namespace ClinicIntelligente.Core.Application.DTOs.Specialty;

public record SpecialtyDto(int Id, string Name, string? Description);

public record CreateSpecialtyDto(
    [Required] string Name,
               string? Description
);