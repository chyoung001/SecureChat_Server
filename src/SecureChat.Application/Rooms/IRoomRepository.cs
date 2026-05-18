using SecureChat.Domain.Entities;

namespace SecureChat.Application.Rooms;

public interface IRoomRepository
{
    Task<Room?> FindByIdAsync(Guid roomId, CancellationToken ct = default);
    Task<Room?> FindByIdWithMembersAsync(Guid roomId, CancellationToken ct = default);
    Task<List<Room>> GetMyRoomsAsync(Guid userId, CancellationToken ct = default);
    Task<Room?> FindDirectRoomAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
    Task<List<Guid>> GetMemberIdsAsync(Guid roomId, CancellationToken ct = default);
    Task<RoomMember?> GetMemberAsync(Guid roomId, Guid userId, CancellationToken ct = default);
    Task<bool> IsMemberAsync(Guid roomId, Guid userId, CancellationToken ct = default);
    Task AddAsync(Room room, CancellationToken ct = default);
    Task AddMemberAsync(RoomMember member, CancellationToken ct = default);
    void RemoveMember(RoomMember member);
    void RemoveRoom(Room room);
}
