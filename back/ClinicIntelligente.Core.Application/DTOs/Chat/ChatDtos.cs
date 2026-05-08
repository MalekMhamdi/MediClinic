namespace ClinicIntelligente.Core.Application.DTOs.Chat;

public record ChatMessageDto(
    int      Id,
    int      SenderId,
    string   SenderName,
    int      ReceiverId,
    string   ReceiverName,
    string   Content,
    DateTime SentAt,
    bool     IsRead
);

public record ChatContactDto(
    int       UserId,
    string    FullName,
    string    Role,
    int       UnreadCount,
    string?   LastMessage,
    DateTime? LastMessageAt
);

public record SendMessageDto(int ReceiverId, string Content);
