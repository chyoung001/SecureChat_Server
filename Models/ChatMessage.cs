namespace SecureChat.Models;

public class ChatMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string PlainText { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public int TtlSeconds { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public MessageStatus Status { get; set; }
    public bool IsMine { get; set; }
    // store 삽입 순서. SentAt 동점 시 정렬 기준으로 사용.
    public long StoreSeq { get; set; }
}
