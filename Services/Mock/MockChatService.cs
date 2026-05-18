using SecureChat.Common;
using SecureChat.Models;

namespace SecureChat.Services.Mock;

public class MockChatService : IChatService
{
    private readonly IAuthService _authService;
    private readonly IMessageStore _messageStore;
    private ConnectionState _state = ConnectionState.Disconnected;
    private static readonly Random _random = new();

    public MockChatService(IAuthService authService, IMessageStore messageStore)
    {
        _authService = authService;
        _messageStore = messageStore;
    }

    public event EventHandler<ChatMessage>? MessageReceived;
    // Mock에서는 발생시키지 않음 — 실제 백엔드 연동 시 구현 예정
#pragma warning disable CS0067
    public event EventHandler<string>? MessageExpired;
    public event EventHandler<string>? ScreenCaptureDetected;
    public event EventHandler<FriendRequestNotification>? FriendRequestReceived;
    public event EventHandler<FriendRequestNotification>? FriendRequestResponded;
    public event EventHandler<string>? NewRoomReceived;
    public event EventHandler<string>? SessionInvalidated;
    public event EventHandler<string>? KickedFromRoom;
    public event EventHandler<string>? RoomMemberChanged;
    public event EventHandler<string>? AdminTransferred;
#pragma warning restore CS0067
    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    public ConnectionState State => _state;

    public async Task ConnectAsync()
    {
        SetState(ConnectionState.Connecting);
        await Task.Delay(300);
        SetState(ConnectionState.Connected);
    }

    public Task DisconnectAsync()
    {
        SetState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    public async Task SendMessageAsync(string roomId, string plainText, int ttlSeconds = 0, string? localMessageId = null)
    {
        await Task.Delay(150);

        await Task.Run(async () =>
        {
            await Task.Delay(500);
            var fakeSenders = new[]
            {
                ("u-kim", "김철수"),
                ("u-lee", "이영희"),
                ("u-park", "박민수")
            };
            var (sid, sname) = fakeSenders[_random.Next(fakeSenders.Length)];

            var echo = new ChatMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                RoomId = roomId,
                SenderId = sid,
                SenderName = sname,
                PlainText = $"(echo) {plainText}",
                SentAt = DateTime.Now,
                TtlSeconds = ttlSeconds,
                ExpiresAt = ttlSeconds > 0 ? DateTime.Now.AddSeconds(ttlSeconds) : null,
                Status = MessageStatus.Delivered,
                IsMine = false
            };
            _messageStore.AddMessage(echo);
            SyncContextHelper.Post(() => MessageReceived?.Invoke(this, echo));
        });
    }

    public Task NotifyScreenCaptureAttemptAsync(string roomId) => Task.CompletedTask;

    private void SetState(ConnectionState newState)
    {
        _state = newState;
        SyncContextHelper.Post(() => ConnectionStateChanged?.Invoke(this, newState));
    }
}
