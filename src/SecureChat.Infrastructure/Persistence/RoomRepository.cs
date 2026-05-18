using Microsoft.EntityFrameworkCore;
using SecureChat.Application.Rooms;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Persistence;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _db;

    public RoomRepository(AppDbContext db) => _db = db;

    public Task<Room?> FindByIdAsync(Guid roomId, CancellationToken ct = default) =>
        _db.Rooms.FindAsync([roomId], ct).AsTask();

    public Task<Room?> FindByIdWithMembersAsync(Guid roomId, CancellationToken ct = default) =>
        _db.Rooms
            .Include(r => r.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Id == roomId, ct);

    public Task<List<Room>> GetMyRoomsAsync(Guid userId, CancellationToken ct = default) =>
        _db.Rooms
            .Where(r => r.Members.Any(m => m.UserId == userId))
            .Include(r => r.Members).ThenInclude(m => m.User)
            .OrderByDescending(r => r.LastMessageAt ?? r.CreatedAt)
            .ToListAsync(ct);

    public Task<Room?> FindDirectRoomAsync(Guid userId1, Guid userId2, CancellationToken ct = default) =>
        _db.Rooms
            .Include(r => r.Members).ThenInclude(m => m.User)
            .Where(r => r.IsDirect
                && r.Members.Any(m => m.UserId == userId1)
                && r.Members.Any(m => m.UserId == userId2))
            .FirstOrDefaultAsync(ct);

    public async Task<List<Guid>> GetMemberIdsAsync(Guid roomId, CancellationToken ct = default) =>
        await _db.RoomMembers
            .Where(rm => rm.RoomId == roomId)
            .Select(rm => rm.UserId)
            .ToListAsync(ct);

    public Task<RoomMember?> GetMemberAsync(Guid roomId, Guid userId, CancellationToken ct = default) =>
        _db.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.UserId == userId, ct);

    public Task<bool> IsMemberAsync(Guid roomId, Guid userId, CancellationToken ct = default) =>
        _db.RoomMembers.AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId, ct);

    public async Task AddAsync(Room room, CancellationToken ct = default) =>
        await _db.Rooms.AddAsync(room, ct);

    public async Task AddMemberAsync(RoomMember member, CancellationToken ct = default) =>
        await _db.RoomMembers.AddAsync(member, ct);

    public void RemoveMember(RoomMember member) =>
        _db.RoomMembers.Remove(member);

    public void RemoveRoom(Room room) =>
        _db.Rooms.Remove(room);
}
