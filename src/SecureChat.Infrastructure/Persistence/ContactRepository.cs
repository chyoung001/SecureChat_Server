using Microsoft.EntityFrameworkCore;
using SecureChat.Application.Contacts;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Persistence;

public class ContactRepository : IContactRepository
{
    private readonly AppDbContext _db;

    public ContactRepository(AppDbContext db) => _db = db;

    public Task<List<Contact>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
        _db.Contacts
            .Include(c => c.ContactUser)
            .Where(c => c.OwnerUserId == ownerUserId)
            .ToListAsync(ct);

    public Task<Contact?> FindAsync(Guid ownerUserId, Guid contactUserId, CancellationToken ct = default) =>
        _db.Contacts
            .FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.ContactUserId == contactUserId, ct);

    public async Task AddAsync(Contact contact, CancellationToken ct = default) =>
        await _db.Contacts.AddAsync(contact, ct);

    public void Remove(Contact contact) =>
        _db.Contacts.Remove(contact);
}
