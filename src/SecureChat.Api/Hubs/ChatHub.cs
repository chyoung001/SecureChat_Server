using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SecureChat.Application.Messages;

namespace SecureChat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IMessageService messageService, ILogger<ChatHub> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    // ── 연결 / 해제 ───────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier!;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        _logger.LogInformation("User {UserId} connected — ConnectionId={ConnectionId}",
            userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier!;
        if (exception is null)
            _logger.LogInformation("User {UserId} disconnected — ConnectionId={ConnectionId}",
                userId, Context.ConnectionId);
        else
            _logger.LogWarning(exception,
                "User {UserId} disconnected with error — ConnectionId={ConnectionId}",
                userId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ── Client → Server ───────────────────────────────────────────────────

    public async Task JoinRoom(Guid roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}");
        _logger.LogInformation("User {UserId} joined room {RoomId}", Context.UserIdentifier, roomId);
    }

    public async Task LeaveRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomId}");
        _logger.LogInformation("User {UserId} left room {RoomId}", Context.UserIdentifier, roomId);
    }

    public async Task<Guid> SendEncryptedMessage(SendMessageRequest request)
    {
        var callerId = Guid.Parse(Context.UserIdentifier!);
        var result = await _messageService.SendEncryptedMessageAsync(callerId, request);
        if (!result.IsSuccess)
            throw new HubException(result.Error);
        return result.Value!;
    }

    public async Task AckDelivery(Guid messageId)
    {
        var callerId = Guid.Parse(Context.UserIdentifier!);
        var result = await _messageService.AckDeliveryAsync(callerId, messageId);
        if (!result.IsSuccess)
            throw new HubException(result.Error);
    }

    public async Task MarkAsRead(Guid roomId, Guid lastReadMessageId)
    {
        var callerId = Guid.Parse(Context.UserIdentifier!);
        var result = await _messageService.MarkAsReadAsync(callerId, roomId, lastReadMessageId);
        if (!result.IsSuccess)
            throw new HubException(result.Error);
    }

    public async Task NotifyScreenCapture(Guid roomId)
    {
        var callerId = Guid.Parse(Context.UserIdentifier!);
        await Clients.Group($"room:{roomId}")
            .SendAsync("ScreenCaptureDetected", new { RoomId = roomId, ByUserId = callerId });
        _logger.LogInformation("ScreenCapture detected by {UserId} in room {RoomId}", callerId, roomId);
    }
}
