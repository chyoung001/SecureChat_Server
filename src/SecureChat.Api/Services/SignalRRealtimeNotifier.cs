using Microsoft.AspNetCore.SignalR;
using SecureChat.Application.Abstractions;
using SecureChat.Api.Hubs;

namespace SecureChat.Api.Services;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<ChatHub> hub) => _hub = hub;

    public Task SendToRoomAsync(Guid roomId, string method, object payload, CancellationToken ct = default) =>
        _hub.Clients.Group($"room:{roomId}").SendAsync(method, payload, cancellationToken: ct);

    public Task SendToUserAsync(Guid userId, string method, object payload, CancellationToken ct = default) =>
        _hub.Clients.Group($"user:{userId}").SendAsync(method, payload, cancellationToken: ct);
}
