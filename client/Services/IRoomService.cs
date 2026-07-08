using SecureChat.Models;

namespace SecureChat.Services;

public interface IRoomService
{
    Task<List<ChatRoom>> GetMyRoomsAsync();
    Task<ChatRoom?> GetRoomAsync(string roomId);
    Task<ChatRoom> CreateRoomAsync(string name, List<string> memberIds);
    Task<ChatRoom> CreateDirectMessageAsync(string targetUserId);
    Task JoinRoomAsync(string roomId);
    Task LeaveRoomAsync(string roomId);
    Task<bool> InviteMemberAsync(string roomId, string targetUserId);
    Task<bool> KickMemberAsync(string roomId, string targetUserId);
    Task<bool> TransferAdminAsync(string roomId, string newAdminUserId);
}
