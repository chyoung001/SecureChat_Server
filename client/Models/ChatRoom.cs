namespace SecureChat.Models;

public class ChatRoom
{
    public string RoomId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDirectMessage { get; set; }
    public List<string> MemberIds { get; set; } = new();
    public List<RoomMember> Members { get; set; } = new();
    public DateTime LastActivityAt { get; set; }
    public int UnreadCount { get; set; }
    public ChatMessage? LastMessagePreview { get; set; }
    public string? LastMessageSender { get; set; }
    public string? LastMessageText { get; set; }
    public bool IsEphemeral { get; set; }
    public string? EphemeralDisplay { get; set; }
}

public class RoomMember
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime LastSeenAt { get; set; }
}
