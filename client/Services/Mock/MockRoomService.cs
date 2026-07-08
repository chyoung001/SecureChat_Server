using SecureChat.Models;

namespace SecureChat.Services.Mock;

public class MockRoomService : IRoomService
{
    private readonly List<ChatRoom> _rooms;

    public MockRoomService()
    {
        var now = DateTime.Now;
        _rooms = new List<ChatRoom>
        {
            new ChatRoom
            {
                RoomId = "room-1",
                Name = "이도현",
                IsDirectMessage = true,
                MemberIds = new() { "u1", "u-lee" },
                LastActivityAt = now.AddSeconds(-20),
                UnreadCount = 3,
                IsEphemeral = true,
                EphemeralDisplay = "30s",
                LastMessageText = "30초 후 사라집니다"
            },
            new ChatRoom
            {
                RoomId = "room-2",
                Name = "데브팀 #보안",
                IsDirectMessage = false,
                MemberIds = new() { "u1", "u2", "u3", "u4" },
                LastActivityAt = now.AddMinutes(-4),
                UnreadCount = 8,
                LastMessageSender = "정민",
                LastMessageText = "배포 완료했어요 ✓"
            },
            new ChatRoom
            {
                RoomId = "room-3",
                Name = "효진",
                IsDirectMessage = true,
                MemberIds = new() { "u1", "u-hj" },
                LastActivityAt = now.AddMinutes(-11),
                UnreadCount = 0,
                LastMessageText = "✓ 내일 점심 어때요?"
            },
            new ChatRoom
            {
                RoomId = "room-4",
                Name = "박준호",
                IsDirectMessage = true,
                MemberIds = new() { "u1", "u-park" },
                LastActivityAt = now.AddHours(-1),
                UnreadCount = 0,
                LastMessageText = "🔒 메시지가 만료되었습니다"
            },
            new ChatRoom
            {
                RoomId = "room-5",
                Name = "보안팀 알림",
                IsDirectMessage = false,
                MemberIds = new() { "u1", "u2", "u3" },
                LastActivityAt = now.AddHours(-3),
                UnreadCount = 1,
                LastMessageText = "⚠ 새 디바이스 로그인 감지"
            },
            new ChatRoom
            {
                RoomId = "room-6",
                Name = "최서연",
                IsDirectMessage = true,
                MemberIds = new() { "u1", "u-choi" },
                LastActivityAt = now.AddDays(-1),
                UnreadCount = 0,
                LastMessageText = "회의 일정 확인 부탁드려요"
            },
            new ChatRoom
            {
                RoomId = "room-7",
                Name = "스터디 그룹",
                IsDirectMessage = false,
                MemberIds = new() { "u1", "u2", "u3", "u4", "u5", "u6" },
                LastActivityAt = now.AddDays(-1).AddHours(-2),
                UnreadCount = 0,
                LastMessageSender = "도현",
                LastMessageText = "다음주 발표 자료..."
            },
            new ChatRoom
            {
                RoomId = "room-8",
                Name = "한지원",
                IsDirectMessage = true,
                MemberIds = new() { "u1", "u-han" },
                LastActivityAt = now.AddDays(-2),
                UnreadCount = 0,
                LastMessageText = "감사합니다!"
            }
        };
    }

    public Task<List<ChatRoom>> GetMyRoomsAsync()
    {
        return Task.FromResult(_rooms
            .OrderByDescending(r => r.LastActivityAt)
            .ToList());
    }

    public Task<ChatRoom?> GetRoomAsync(string roomId)
    {
        return Task.FromResult(_rooms.FirstOrDefault(r => r.RoomId == roomId));
    }

    public Task<ChatRoom> CreateRoomAsync(string name, List<string> memberIds)
    {
        var room = new ChatRoom
        {
            RoomId = Guid.NewGuid().ToString(),
            Name = name,
            IsDirectMessage = false,
            MemberIds = memberIds,
            LastActivityAt = DateTime.Now,
            UnreadCount = 0
        };
        _rooms.Add(room);
        return Task.FromResult(room);
    }

    public Task<ChatRoom> CreateDirectMessageAsync(string targetUserId)
    {
        // 이미 해당 유저와의 1:1 방이 있으면 재사용
        var existing = _rooms.FirstOrDefault(r =>
            r.IsDirectMessage && r.MemberIds.Contains(targetUserId));
        if (existing != null)
            return Task.FromResult(existing);

        var room = new ChatRoom
        {
            RoomId          = Guid.NewGuid().ToString(),
            Name            = $"1:1 with {targetUserId}",
            IsDirectMessage = true,
            MemberIds       = new() { targetUserId },
            LastActivityAt  = DateTime.Now,
            UnreadCount     = 0
        };
        _rooms.Add(room);
        return Task.FromResult(room);
    }

    public Task JoinRoomAsync(string roomId) => Task.CompletedTask;
    public Task LeaveRoomAsync(string roomId) => Task.CompletedTask;
    public Task<bool> InviteMemberAsync(string roomId, string targetUserId) => Task.FromResult(true);
    public Task<bool> KickMemberAsync(string roomId, string targetUserId) => Task.FromResult(true);
    public Task<bool> TransferAdminAsync(string roomId, string newAdminUserId) => Task.FromResult(true);
}
