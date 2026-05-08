namespace ClinicIntelligente.Core.Application.Interfaces.Services;

using ClinicIntelligente.Core.Application.DTOs.Ai;

public interface IAiOrientationService
{
    Task<AiSessionResponseDto> CreateSessionAsync();
    Task<AiCompletedResponseDto> ChatAsync(AiChatMessageDto dto);
    Task ResetSessionAsync(string sessionId);
    Task<AiUploadResponseDto> UploadContextAsync(string sessionId, Stream fileStream, string fileName, string contentType);
}