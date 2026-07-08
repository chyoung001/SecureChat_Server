using SecureChat.Application.Common;
using SecureChat.Application.Contacts;
using SecureChat.Application.FriendRequests;
using SecureChat.Application.Messages;
using SecureChat.Application.Rooms;
using SecureChat.Application.Users;

namespace SecureChat.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(
        AppDbContext db,
        IUserRepository users,
        IRoomRepository rooms,
        IMessageRepository messages,
        IContactRepository contacts,
        IFriendRequestRepository friendRequests)
    {
        _db = db;
        Users = users;
        Rooms = rooms;
        Messages = messages;
        Contacts = contacts;
        FriendRequests = friendRequests;
    }

    public IUserRepository Users { get; }
    public IRoomRepository Rooms { get; }
    public IMessageRepository Messages { get; }
    public IContactRepository Contacts { get; }
    public IFriendRequestRepository FriendRequests { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
