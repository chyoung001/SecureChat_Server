using Microsoft.Extensions.Logging;
using SecureChat.Application.Abstractions;
using SecureChat.Application.Common;
using SecureChat.Application.Messages;
using SecureChat.Application.Users;
using SecureChat.Domain.Entities;

namespace SecureChat.Application.Rooms;

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _notifier;
    private readonly ILogger<RoomService> _logger;

    public RoomService(IUnitOfWork uow, IRealtimeNotifier notifier, ILogger<RoomService> logger)
    {
        _uow = uow;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<Result<List<RoomSummaryDto>>> GetMyRoomsAsync(Guid userId, CancellationToken ct = default)
    {
        var rooms = await _uow.Rooms.GetMyRoomsAsync(userId, ct);
        var summaries = new List<RoomSummaryDto>();

        foreach (var room in rooms)
        {
            var member = room.Members.FirstOrDefault(m => m.UserId == userId);

            var (lastMsg, lastKey) = await _uow.Messages.GetLastMessageWithKeyAsync(room.Id, userId, ct);

            EncryptedMessageDto? lastMsgDto = null;
            if (lastMsg != null)
                lastMsgDto = ToMessageDto(lastMsg, lastKey?.EncryptedAesKey ?? string.Empty);

            int unread = 0;
            if (member?.LastReadAt.HasValue == true)
            {
                // LastReadAt 이후 메시지 수 — 읽은 메시지가 만료됐어도 타임스탬프는 남아있음
                unread = await _uow.Messages.CountAfterAsync(room.Id, member.LastReadAt.Value, ct);
            }
            else if (lastMsg != null)
            {
                unread = await _uow.Messages.CountAfterAsync(room.Id, DateTime.MinValue, ct);
            }

            summaries.Add(new RoomSummaryDto(
                room.Id, room.Name, room.IsDirect,
                room.Members.Select(ToUserDto).ToList(),
                lastMsgDto, unread, room.LastMessageAt));
        }

        return Result<List<RoomSummaryDto>>.Success(summaries);
    }

    public async Task<Result<RoomDetailDto>> GetRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default)
    {
        var room = await _uow.Rooms.FindByIdWithMembersAsync(roomId, ct);
        if (room is null)
            return Result<RoomDetailDto>.Failure("Room not found");

        if (!room.Members.Any(m => m.UserId == userId))
            return Result<RoomDetailDto>.Failure("Not a room member");

        return Result<RoomDetailDto>.Success(ToDetailDto(room));
    }

    public async Task<Result<RoomDetailDto>> CreateGroupRoomAsync(
        Guid creatorId, CreateGroupRoomRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<RoomDetailDto>.Failure("Room name is required");

        var uniqueOtherIds = request.MemberIds
            .Distinct().Where(id => id != creatorId).ToList();

        if (uniqueOtherIds.Count + 1 > 50)
            return Result<RoomDetailDto>.Failure("Group rooms support at most 50 members");

        var room = Room.CreateGroup(request.Name.Trim(), creatorId);
        await _uow.Rooms.AddAsync(room, ct);
        await _uow.Rooms.AddMemberAsync(RoomMember.Create(room.Id, creatorId, isAdmin: true), ct);

        foreach (var memberId in uniqueOtherIds)
            await _uow.Rooms.AddMemberAsync(RoomMember.Create(room.Id, memberId), ct);

        await _uow.SaveChangesAsync(ct);

        var created = await _uow.Rooms.FindByIdWithMembersAsync(room.Id, ct);
        var dto = ToDetailDto(created!);

        foreach (var memberId in uniqueOtherIds)
            await _notifier.SendToUserAsync(memberId, "RoomInvited", dto, ct);

        _logger.LogInformation("Group room {RoomId} created by {UserId} with {Count} members",
            room.Id, creatorId, uniqueOtherIds.Count + 1);

        return Result<RoomDetailDto>.Success(dto);
    }

    public async Task<(Result<RoomDetailDto> Result, bool Created)> GetOrCreateDirectRoomAsync(
        Guid userId, Guid otherUserId, CancellationToken ct = default)
    {
        if (userId == otherUserId)
            return (Result<RoomDetailDto>.Failure("Cannot create a direct room with yourself"), false);

        var existing = await _uow.Rooms.FindDirectRoomAsync(userId, otherUserId, ct);
        if (existing != null)
            return (Result<RoomDetailDto>.Success(ToDetailDto(existing)), false);

        var room = Room.CreateDirect(userId);
        await _uow.Rooms.AddAsync(room, ct);
        await _uow.Rooms.AddMemberAsync(RoomMember.Create(room.Id, userId), ct);
        await _uow.Rooms.AddMemberAsync(RoomMember.Create(room.Id, otherUserId), ct);

        await _uow.SaveChangesAsync(ct);

        var created = await _uow.Rooms.FindByIdWithMembersAsync(room.Id, ct);
        var dto = ToDetailDto(created!);

        await _notifier.SendToUserAsync(otherUserId, "RoomInvited", dto, ct);

        return (Result<RoomDetailDto>.Success(dto), true);
    }

    public async Task<Result<RoomDetailDto>> InviteAsync(
        Guid callerId, Guid roomId, Guid targetUserId, CancellationToken ct = default)
    {
        var room = await _uow.Rooms.FindByIdWithMembersAsync(roomId, ct);
        if (room is null)
            return Result<RoomDetailDto>.Failure("Room not found");

        if (!room.Members.Any(m => m.UserId == callerId))
            return Result<RoomDetailDto>.Failure("Not authorized");

        if (room.IsDirect)
            return Result<RoomDetailDto>.Failure("Cannot invite to a direct message room");

        if (room.Members.Any(m => m.UserId == targetUserId))
            return Result<RoomDetailDto>.Failure("User is already a member");

        var targetUser = await _uow.Users.FindByIdAsync(targetUserId, ct);
        if (targetUser is null)
            return Result<RoomDetailDto>.Failure("User not found");

        await _uow.Rooms.AddMemberAsync(RoomMember.Create(roomId, targetUserId), ct);
        await _uow.SaveChangesAsync(ct);

        var updated = await _uow.Rooms.FindByIdWithMembersAsync(roomId, ct);
        var dto = ToDetailDto(updated!);

        await _notifier.SendToUserAsync(targetUserId, "RoomInvited", dto, ct);
        await _notifier.SendToRoomAsync(roomId, "UserJoinedRoom",
            new { RoomId = roomId, UserId = targetUserId, targetUser.Username }, ct);

        _logger.LogInformation("User {TargetId} invited to room {RoomId} by {CallerId}",
            targetUserId, roomId, callerId);

        return Result<RoomDetailDto>.Success(dto);
    }

    public async Task<Result> KickMemberAsync(Guid callerId, Guid roomId, Guid targetUserId, CancellationToken ct = default)
    {
        var room = await _uow.Rooms.FindByIdWithMembersAsync(roomId, ct);
        if (room is null)
            return Result.Failure("Room not found");

        var caller = room.Members.FirstOrDefault(m => m.UserId == callerId);
        if (caller is null || !caller.IsAdmin)
            return Result.Failure("관리자 권한이 없습니다.");

        if (room.IsDirect)
            return Result.Failure("1:1 채팅방에서는 강퇴할 수 없습니다.");

        if (callerId == targetUserId)
            return Result.Failure("자기 자신을 강퇴할 수 없습니다.");

        var target = room.Members.FirstOrDefault(m => m.UserId == targetUserId);
        if (target is null)
            return Result.Failure("해당 멤버를 찾을 수 없습니다.");

        if (target.IsAdmin)
            return Result.Failure("방장은 강퇴할 수 없습니다.");

        _uow.Rooms.RemoveMember(target);
        await _uow.SaveChangesAsync(ct);

        await _notifier.SendToUserAsync(targetUserId, "KickedFromRoom",
            new { RoomId = roomId }, ct);
        await _notifier.SendToRoomAsync(roomId, "UserLeftRoom",
            new { RoomId = roomId, UserId = targetUserId }, ct);

        _logger.LogInformation("User {TargetId} kicked from room {RoomId} by {CallerId}",
            targetUserId, roomId, callerId);

        return Result.Success();
    }

    public async Task<Result> TransferAdminAsync(Guid callerId, Guid roomId, Guid newAdminUserId, CancellationToken ct = default)
    {
        var room = await _uow.Rooms.FindByIdWithMembersAsync(roomId, ct);
        if (room is null)
            return Result.Failure("Room not found");

        if (room.IsDirect)
            return Result.Failure("1:1 채팅방에는 방장이 없습니다.");

        var caller = room.Members.FirstOrDefault(m => m.UserId == callerId);
        if (caller is null || !caller.IsAdmin)
            return Result.Failure("방장만 권한을 위임할 수 있습니다.");

        if (callerId == newAdminUserId)
            return Result.Failure("자기 자신에게는 위임할 수 없습니다.");

        var target = room.Members.FirstOrDefault(m => m.UserId == newAdminUserId);
        if (target is null)
            return Result.Failure("해당 멤버를 찾을 수 없습니다.");

        caller.DemoteFromAdmin();
        target.PromoteToAdmin();
        await _uow.SaveChangesAsync(ct);

        await _notifier.SendToRoomAsync(roomId, "AdminTransferred",
            new { RoomId = roomId, NewAdminUserId = newAdminUserId, PrevAdminUserId = callerId }, ct);

        _logger.LogInformation("Admin transferred from {PrevId} to {NewId} in room {RoomId}",
            callerId, newAdminUserId, roomId);

        return Result.Success();
    }

    public async Task<Result> LeaveRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default)
    {
        var room = await _uow.Rooms.FindByIdWithMembersAsync(roomId, ct);
        if (room is null)
            return Result.Failure("Not a room member");

        var member = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            return Result.Failure("Not a room member");

        // 방장이 나가려는 경우: 다른 멤버가 있으면 먼저 위임 필요
        if (!room.IsDirect && member.IsAdmin)
        {
            var others = room.Members.Where(m => m.UserId != userId).ToList();
            if (others.Count > 0)
                return Result.Failure("ADMIN_MUST_TRANSFER");
            // 방에 혼자 남은 방장 → 그냥 나가서 방 삭제 처리 (멤버 0명)
        }

        _uow.Rooms.RemoveMember(member);

        var remainingCount = room.Members.Count(m => m.UserId != userId);
        if (remainingCount == 0)
        {
            _uow.Rooms.RemoveRoom(room);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Room {RoomId} deleted — no members remaining", roomId);
            return Result.Success();
        }

        await _uow.SaveChangesAsync(ct);

        await _notifier.SendToRoomAsync(roomId, "UserLeftRoom",
            new { RoomId = roomId, UserId = userId }, ct);

        _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
        return Result.Success();
    }

    // ── 매핑 헬퍼 ──────────────────────────────────────────────────────────

    private static RoomDetailDto ToDetailDto(Room room) => new(
        room.Id, room.Name, room.IsDirect, room.CreatedById, room.CreatedAt,
        room.Members.Select(m => new RoomMemberDto(
            m.UserId, m.User.Username, m.User.DisplayName, m.IsAdmin, m.JoinedAt, m.User.LastSeenAt
        )).ToList());

    private static UserDto ToUserDto(RoomMember m) =>
        new(m.User.Id, m.User.Username, m.User.DisplayName,
            m.User.Email, m.User.KeyFingerprint, m.User.LastSeenAt);

    private static EncryptedMessageDto ToMessageDto(Domain.Entities.Message m, string encryptedAesKey) =>
        new(m.Id, m.RoomId, m.SenderId, encryptedAesKey,
            m.Iv, m.Ciphertext, m.HmacTag, m.TtlSeconds, m.SentAt, m.ExpiresAt);
}
