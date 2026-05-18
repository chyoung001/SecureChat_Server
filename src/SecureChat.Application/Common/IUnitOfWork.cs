using SecureChat.Application.Contacts;
using SecureChat.Application.FriendRequests;
using SecureChat.Application.Messages;
using SecureChat.Application.Rooms;
using SecureChat.Application.Users;

namespace SecureChat.Application.Common;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IRoomRepository Rooms { get; }
    IMessageRepository Messages { get; }
    IContactRepository Contacts { get; }
    IFriendRequestRepository FriendRequests { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
