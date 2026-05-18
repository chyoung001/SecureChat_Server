using SecureChat.Application.Messages;
using SecureChat.Application.Users;

namespace SecureChat.Application.Rooms;

public record RoomSummaryDto(
    Guid Id,
    string? Name,
    bool IsDirect,
    List<UserDto> Members,
    EncryptedMessageDto? LastMessage,
    int UnreadCount,
    DateTime? LastMessageAt);

public record RoomDetailDto(
    Guid Id,
    string? Name,
    bool IsDirect,
    Guid CreatedById,
    DateTime CreatedAt,
    List<RoomMemberDto> Members);

public record RoomMemberDto(
    Guid UserId,
    string Username,
    string DisplayName,
    bool IsAdmin,
    DateTime JoinedAt,
    DateTime LastSeenAt);

public record CreateGroupRoomRequest(string Name, List<Guid> MemberIds);
public record CreateDirectRoomRequest(Guid OtherUserId);
public record InviteMemberRequest(Guid UserId);
public record TransferAdminRequest(Guid NewAdminUserId);
