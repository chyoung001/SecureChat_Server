namespace SecureChat.Application.Messages;

public record SendMessageRequest(
    Guid RoomId,
    string Iv,
    string Ciphertext,
    string HmacTag,
    int TtlSeconds,
    List<MessageKeyEntry> Keys
);

public record MessageKeyEntry(Guid RecipientUserId, string EncryptedAesKey);

public record EncryptedMessageDto(
    Guid Id,
    Guid RoomId,
    Guid SenderId,
    string EncryptedAesKey,   // 수신자 본인 키로 필터링됨
    string Iv,
    string Ciphertext,
    string HmacTag,
    int TtlSeconds,
    DateTime SentAt,
    DateTime ExpiresAt
);

public record MessagePageDto(List<EncryptedMessageDto> Items, DateTime? NextCursor);

public record MessageStatusChangedDto(Guid MessageId, string Status);   // Delivered 전용
public record ReadReceiptDto(Guid RoomId, Guid RecipientUserId, Guid LastReadMessageId, DateTime ReadAt);
public record MessageExpiredDto(List<Guid> MessageIds, Guid RoomId);
