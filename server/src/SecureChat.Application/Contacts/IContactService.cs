using SecureChat.Application.Common;

namespace SecureChat.Application.Contacts;

public interface IContactService
{
    Task<Result<List<ContactDto>>> GetMyContactsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<ContactDto>> AddContactAsync(Guid ownerUserId, Guid contactUserId, CancellationToken ct = default);
    Task<Result> RemoveContactAsync(Guid ownerUserId, Guid contactUserId, CancellationToken ct = default);
    Task<Result<ContactDto>> ToggleBlockAsync(Guid ownerUserId, Guid contactUserId, bool isBlocked, CancellationToken ct = default);
}
