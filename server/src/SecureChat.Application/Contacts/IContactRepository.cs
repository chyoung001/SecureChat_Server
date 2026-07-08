using SecureChat.Domain.Entities;

namespace SecureChat.Application.Contacts;

public interface IContactRepository
{
    Task<List<Contact>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default);
    Task<Contact?> FindAsync(Guid ownerUserId, Guid contactUserId, CancellationToken ct = default);
    Task AddAsync(Contact contact, CancellationToken ct = default);
    void Remove(Contact contact);
}
