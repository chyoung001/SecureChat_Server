using SecureChat.Models;

namespace SecureChat.Services;

public interface IChatService
{
    event EventHandler<ChatMessage>? MessageReceived;
    event EventHandler<string>? MessageExpired;
    event EventHandler<ConnectionState>? ConnectionStateChanged;
    event EventHandler<string>? ScreenCaptureDetected;
    event EventHandler<FriendRequestNotification>? FriendRequestReceived;
    event EventHandler<FriendRequestNotification>? FriendRequestResponded;
    event EventHandler<string>? NewRoomReceived;
    event EventHandler<string>? SessionInvalidated;
    event EventHandler<string>? KickedFromRoom;
    // roomId — 멤버가 입장하거나 퇴장한 방
    event EventHandler<string>? RoomMemberChanged;
    // roomId — 방장이 변경된 방
    event EventHandler<string>? AdminTransferred;

    Task ConnectAsync();
    Task DisconnectAsync();
    Task SendMessageAsync(string roomId, string plainText, int ttlSeconds = 0, string? localMessageId = null);
    Task NotifyScreenCaptureAttemptAsync(string roomId);
    ConnectionState State { get; }
}
