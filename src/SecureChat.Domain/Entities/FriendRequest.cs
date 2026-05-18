using SecureChat.Domain.Common;
using SecureChat.Domain.Enums;

namespace SecureChat.Domain.Entities;

public class FriendRequest : EntityBase
{
    public Guid FromUserId { get; private set; }
    public Guid ToUserId { get; private set; }
    public FriendRequestStatus Status { get; private set; }
    public string? Message { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    public User FromUser { get; private set; } = null!;
    public User ToUser { get; private set; } = null!;

    private FriendRequest() { }

    public static FriendRequest Create(Guid fromUserId, Guid toUserId, string? message = null)
    {
        return new FriendRequest
        {
            Id = Guid.NewGuid(),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Status = FriendRequestStatus.Pending,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Accept()
    {
        Status = FriendRequestStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = FriendRequestStatus.Rejected;
        RespondedAt = DateTime.UtcNow;
    }
}
