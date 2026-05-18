using Microsoft.EntityFrameworkCore;
using SecureChat.Application.FriendRequests;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Enums;

namespace SecureChat.Infrastructure.Persistence;

public class FriendRequestRepository : IFriendRequestRepository
{
    private readonly AppDbContext _db;

    public FriendRequestRepository(AppDbContext db) => _db = db;

    public Task<FriendRequest?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.FriendRequests
           .Include(fr => fr.FromUser)
           .Include(fr => fr.ToUser)
           .FirstOrDefaultAsync(fr => fr.Id == id, ct);

    public Task<FriendRequest?> FindPendingAsync(Guid fromUserId, Guid toUserId, CancellationToken ct = default) =>
        _db.FriendRequests
           .FirstOrDefaultAsync(fr =>
               fr.FromUserId == fromUserId &&
               fr.ToUserId   == toUserId   &&
               fr.Status     == FriendRequestStatus.Pending, ct);

    public Task<List<FriendRequest>> GetIncomingPendingAsync(Guid toUserId, CancellationToken ct = default) =>
        _db.FriendRequests
           .Include(fr => fr.FromUser)
           .Include(fr => fr.ToUser)
           .Where(fr => fr.ToUserId == toUserId && fr.Status == FriendRequestStatus.Pending)
           .OrderByDescending(fr => fr.CreatedAt)
           .ToListAsync(ct);

    public Task<List<FriendRequest>> GetOutgoingPendingAsync(Guid fromUserId, CancellationToken ct = default) =>
        _db.FriendRequests
           .Include(fr => fr.FromUser)
           .Include(fr => fr.ToUser)
           .Where(fr => fr.FromUserId == fromUserId && fr.Status == FriendRequestStatus.Pending)
           .OrderByDescending(fr => fr.CreatedAt)
           .ToListAsync(ct);

    public async Task AddAsync(FriendRequest request, CancellationToken ct = default) =>
        await _db.FriendRequests.AddAsync(request, ct);
}
