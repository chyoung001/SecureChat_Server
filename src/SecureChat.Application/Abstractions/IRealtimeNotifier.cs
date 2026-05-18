namespace SecureChat.Application.Abstractions;

public interface IRealtimeNotifier
{
    Task SendToRoomAsync(Guid roomId, string method, object payload, CancellationToken ct = default);
    Task SendToUserAsync(Guid userId, string method, object payload, CancellationToken ct = default);
}
