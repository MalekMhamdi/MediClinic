
namespace ClinicIntelligente.Core.Application.Services;

using ClinicIntelligente.Core.Application.DTOs.Ai;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

public class AiOrientationService : IAiOrientationService
{
    private readonly HttpClient _http;
    private readonly IDoctorRepository _doctorRepo;
    private readonly ISpecialtyRepository _specialtyRepo;
    private readonly string _aiBaseUrl;

    public AiOrientationService(
        HttpClient http,
        IDoctorRepository doctorRepo,
        ISpecialtyRepository specialtyRepo,
        IConfiguration config)
    {
        _http = http;
        _doctorRepo = doctorRepo;
        _specialtyRepo = specialtyRepo;
        _aiBaseUrl = config["AiService:BaseUrl"] ?? "http://localhost:8000";
    }

    public async Task<AiSessionResponseDto> CreateSessionAsync()
    {
        var response = await _http.GetFromJsonAsync<AiSessionResponseDto>(
            $"{_aiBaseUrl}/api/new-session");

        return response ?? throw new Exception("AI service did not return a session.");
    }

    public async Task<AiCompletedResponseDto> ChatAsync(AiChatMessageDto dto)
    {
        var payload = new AiOrientationRequestDto(dto.SessionId, dto.UserMessage, dto.Language, dto.PatientName);

        var httpResponse = await _http.PostAsJsonAsync($"{_aiBaseUrl}/api/orientation", payload);
        httpResponse.EnsureSuccessStatusCode();

        var aiResponse = await httpResponse.Content.ReadFromJsonAsync<AiOrientationResponseDto>()
            ?? throw new Exception("AI service returned an empty response.");

        // If orientation is complete, try to find a matching doctor
        int? suggestedDoctorId = null;
        string suggestedDoctorName = "";
        string suggestedSpecialty = aiResponse.Result?.MedicalSpecialty ?? "";

        if (aiResponse.IsComplete && aiResponse.Result is not null)
        {
            var specialtyName = aiResponse.Result.MedicalSpecialty;
            var specialties = await _specialtyRepo.GetAllAsync();
            var matched = specialties.FirstOrDefault(s =>
                s.Name.Contains(specialtyName, StringComparison.OrdinalIgnoreCase));

            if (matched is not null)
            {
                var doctors = await _doctorRepo.GetBySpecialtyAsync(matched.Id);
                var doctor = doctors.FirstOrDefault(d => d.IsActive);
                if (doctor is not null)
                {
                    suggestedDoctorId = doctor.Id;
                    suggestedDoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}";
                }
            }
        }

        return new AiCompletedResponseDto(
            Reply: aiResponse.Reply,
            IsComplete: aiResponse.IsComplete,
            SessionStep: aiResponse.SessionStep,
            Result: aiResponse.Result,
            SuggestedDoctorId: suggestedDoctorId,
            SuggestedDoctorName: suggestedDoctorName,
            SuggestedSpecialty: suggestedSpecialty
        );
    }

    public async Task ResetSessionAsync(string sessionId)
    {
        await _http.DeleteAsync($"{_aiBaseUrl}/api/orientation/{sessionId}");
    }

    public async Task<AiUploadResponseDto> UploadContextAsync(
        string sessionId, Stream fileStream, string fileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        var response = await _http.PostAsync(
            $"{_aiBaseUrl}/api/orientation/upload-context?session_id={Uri.EscapeDataString(sessionId)}",
            content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AiUploadResponseDto>()
            ?? throw new Exception("AI service returned an empty upload response.");
    }
}