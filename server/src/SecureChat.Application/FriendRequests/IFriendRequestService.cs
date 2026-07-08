using SecureChat.Application.Common;

namespace SecureChat.Application.FriendRequests;

public interface IFriendRequestService
{
    Task<Result<FriendRequestDto>> SendAsync(Guid fromUserId, SendFriendRequestRequest request, CancellationToken ct = default);
    Task<Result<List<FriendRequestDto>>> GetIncomingAsync(Guid userId, CancellationToken ct = default);
    Task<Result<List<FriendRequestDto>>> GetOutgoingAsync(Guid userId, CancellationToken ct = default);
    Task<Result<FriendRequestDto>> AcceptAsync(Guid callerId, Guid requestId, CancellationToken ct = default);
    Task<Result<FriendRequestDto>> RejectAsync(Guid callerId, Guid requestId, CancellationToken ct = default);
}
