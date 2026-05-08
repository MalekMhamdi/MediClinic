namespace ClinicIntelligente.Core.Application.DTOs.Ai;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public record AiSessionResponseDto(
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("message")] string Message
);

public record AiOrientationRequestDto(
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("user_message")] string UserMessage,
    [property: JsonPropertyName("language")] string Language = "fr",
    [property: JsonPropertyName("patient_name")] string? PatientName = null
);

public record AiOrientationResultDto(
    [property: JsonPropertyName("patient_name")] string PatientName,
    [property: JsonPropertyName("symptoms_summary")] string SymptomsSummary,
    [property: JsonPropertyName("symptoms_duration")] string SymptomsDuration,
    [property: JsonPropertyName("medical_specialty")] string MedicalSpecialty,
    [property: JsonPropertyName("urgency")] string Urgency,
    [property: JsonPropertyName("urgency_reason")] string UrgencyReason,
    [property: JsonPropertyName("response_to_patient")] string ResponseToPatient,
    [property: JsonPropertyName("recommended_action")] string RecommendedAction
);

public record AiOrientationResponseDto(
    [property: JsonPropertyName("reply")] string Reply,
    [property: JsonPropertyName("is_complete")] bool IsComplete,
    [property: JsonPropertyName("result")] AiOrientationResultDto? Result,
    [property: JsonPropertyName("session_step")] int SessionStep,
    [property: JsonPropertyName("tokens_used")] int TokensUsed
);

// What .NET sends back to Angular after IA completion
public record AiCompletedResponseDto(
    string Reply,
    bool IsComplete,
    int SessionStep,
    AiOrientationResultDto? Result,
    // Pre-filled appointment suggestion (if specialty matched a doctor)
    int? SuggestedDoctorId,
    string SuggestedDoctorName,
    string SuggestedSpecialty
);

public record AiChatMessageDto(
    [Required] string SessionId,
    string UserMessage = "",
    string Language = "fr",
    string? PatientName = null
);

public record AiUploadResponseDto(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("extracted_chars")] int ExtractedChars,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("preview")] string Preview
);