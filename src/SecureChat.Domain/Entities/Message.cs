using SecureChat.Domain.Common;

namespace SecureChat.Domain.Entities;

public class Message : EntityBase
{
    public Guid RoomId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Iv { get; private set; } = null!;
    public string Ciphertext { get; private set; } = null!;
    public string HmacTag { get; private set; } = null!;
    public int TtlSeconds { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public ICollection<MessageKey> Keys { get; private set; } = new List<MessageKey>();

    private Message() { }

    public static Message Create(
        Guid roomId, Guid senderId,
        string iv, string ciphertext, string hmacTag,
        int ttlSeconds)
    {
        var sentAt = DateTime.UtcNow;
        // ttlSeconds=0이면 영구 메시지 → DateTime.MaxValue
        var expiresAt = ttlSeconds > 0
            ? sentAt.AddSeconds(ttlSeconds)
            : DateTime.MaxValue;

        return new Message
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            SenderId = senderId,
            Iv = iv,
            Ciphertext = ciphertext,
            HmacTag = hmacTag,
            TtlSeconds = ttlSeconds,
            SentAt = sentAt,
            ExpiresAt = expiresAt
        };
    }
}
