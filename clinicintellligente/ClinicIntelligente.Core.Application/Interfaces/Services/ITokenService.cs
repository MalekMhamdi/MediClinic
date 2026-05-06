using ClinicIntelligente.Core.Domain.Entities;

namespace ClinicIntelligente.Core.Application.Interfaces.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}