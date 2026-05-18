using SecureChat.Domain.Entities;

namespace SecureChat.Application.FriendRequests;

public interface IFriendRequestRepository
{
    Task<FriendRequest?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<FriendRequest?> FindPendingAsync(Guid fromUserId, Guid toUserId, CancellationToken ct = default);
    Task<List<FriendRequest>> GetIncomingPendingAsync(Guid toUserId, CancellationToken ct = default);
    Task<List<FriendRequest>> GetOutgoingPendingAsync(Guid fromUserId, CancellationToken ct = default);
    Task AddAsync(FriendRequest request, CancellationToken ct = default);
}
