using SecureChat.Application.Common;

namespace SecureChat.Application.Rooms;

public interface IRoomService
{
    Task<Result<List<RoomSummaryDto>>> GetMyRoomsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<RoomDetailDto>> GetRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default);
    Task<Result<RoomDetailDto>> CreateGroupRoomAsync(Guid creatorId, CreateGroupRoomRequest request, CancellationToken ct = default);
    Task<(Result<RoomDetailDto> Result, bool Created)> GetOrCreateDirectRoomAsync(Guid userId, Guid otherUserId, CancellationToken ct = default);
    Task<Result> LeaveRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default);
    Task<Result<RoomDetailDto>> InviteAsync(Guid callerId, Guid roomId, Guid targetUserId, CancellationToken ct = default);
    Task<Result> KickMemberAsync(Guid callerId, Guid roomId, Guid targetUserId, CancellationToken ct = default);
    Task<Result> TransferAdminAsync(Guid callerId, Guid roomId, Guid newAdminUserId, CancellationToken ct = default);
}
