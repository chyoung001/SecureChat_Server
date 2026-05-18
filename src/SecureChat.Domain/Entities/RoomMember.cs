namespace SecureChat.Domain.Entities;

public class RoomMember
{
    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsAdmin { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public Guid? LastReadMessageId { get; private set; }
    public DateTime? LastReadAt { get; private set; }

    public Room Room { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private RoomMember() { }

    public static RoomMember Create(Guid roomId, Guid userId, bool isAdmin = false)
    {
        return new RoomMember
        {
            RoomId = roomId,
            UserId = userId,
            IsAdmin = isAdmin,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void UpdateLastRead(Guid messageId, DateTime readAt)
    {
        LastReadMessageId = messageId;
        LastReadAt = readAt;
    }

    public void PromoteToAdmin() => IsAdmin = true;
    public void DemoteFromAdmin() => IsAdmin = false;
}
