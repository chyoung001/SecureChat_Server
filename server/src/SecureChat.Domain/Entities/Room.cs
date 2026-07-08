using SecureChat.Domain.Common;

namespace SecureChat.Domain.Entities;

public class Room : EntityBase
{
    public string? Name { get; private set; }
    public bool IsDirect { get; private set; }
    public Guid CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastMessageAt { get; private set; }

    public ICollection<RoomMember> Members { get; private set; } = new List<RoomMember>();
    public ICollection<Message> Messages { get; private set; } = new List<Message>();

    private Room() { }

    public static Room CreateDirect(Guid createdById)
    {
        return new Room
        {
            Id = Guid.NewGuid(),
            Name = null,
            IsDirect = true,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Room CreateGroup(string name, Guid createdById)
    {
        return new Room
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsDirect = false,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateLastMessageAt(DateTime at) => LastMessageAt = at;
}
