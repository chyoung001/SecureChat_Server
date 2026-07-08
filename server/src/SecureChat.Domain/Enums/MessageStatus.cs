namespace SecureChat.Domain.Enums;

// 클라이언트 전용 — DB에 저장 안 됨
public enum MessageStatus
{
    Sending,
    Sent,
    Delivered,
    Read,
    Failed,
    Expired
}