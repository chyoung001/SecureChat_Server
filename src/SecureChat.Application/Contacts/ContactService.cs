using SecureChat.Application.Common;
using SecureChat.Domain.Entities;

namespace SecureChat.Application.Contacts;

public class ContactService : IContactService
{
    private readonly IUnitOfWork _uow;

    public ContactService(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<List<ContactDto>>> GetMyContactsAsync(Guid userId, CancellationToken ct = default)
    {
        var contacts = await _uow.Contacts.GetByOwnerAsync(userId, ct);
        return Result<List<ContactDto>>.Success(contacts.Select(ToDto).ToList());
    }

    public async Task<Result<ContactDto>> AddContactAsync(Guid ownerUserId, Guid contactUserId, CancellationToken ct = default)
    {
        if (ownerUserId == contactUserId)
            return Result<ContactDto>.Failure("Cannot add yourself as a contact");

        var existing = await _uow.Contacts.FindAsync(ownerUserId, contactUserId, ct);
        if (existing != null)
            return Result<ContactDto>.Failure("Contact already exists");

        var contactUser = await _uow.Users.FindByIdAsync(contactUserId, ct);
        if (contactUser is null)
            return Result<ContactDto>.Failure("User not found");

        var contact = Contact.Create(ownerUserId, contactUserId);
        await _uow.Contacts.AddAsync(contact, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<ContactDto>.Success(new ContactDto(
            contactUser.Id, contactUser.Username, contactUser.DisplayName,
            contact.Nickname, contact.IsBlocked));
    }

    public async Task<Result> RemoveContactAsync(Guid ownerUserId, Guid contactUserId, CancellationToken ct = default)
    {
        var contact = await _uow.Contacts.FindAsync(ownerUserId, contactUserId, ct);
        if (contact is null) return Result.Failure("Contact not found");

        // 양방향 모두 삭제 — 한쪽만 지우면 상대방 연락처에 남아 재추가가 막힘
        _uow.Contacts.Remove(contact);
        var reverse = await _uow.Contacts.FindAsync(contactUserId, ownerUserId, ct);
        if (reverse is not null)
            _uow.Contacts.Remove(reverse);

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ContactDto>> ToggleBlockAsync(Guid ownerUserId, Guid contactUserId, bool isBlocked, CancellationToken ct = default)
    {
        var contact = await _uow.Contacts.FindAsync(ownerUserId, contactUserId, ct);
        if (contact is null) return Result<ContactDto>.Failure("Contact not found");

        contact.SetBlocked(isBlocked);
        await _uow.SaveChangesAsync(ct);

        var contactUser = await _uow.Users.FindByIdAsync(contactUserId, ct);
        return Result<ContactDto>.Success(new ContactDto(
            contactUser!.Id, contactUser.Username, contactUser.DisplayName,
            contact.Nickname, contact.IsBlocked));
    }

    private static ContactDto ToDto(Contact c) => new(
        c.ContactUser.Id, c.ContactUser.Username, c.ContactUser.DisplayName,
        c.Nickname, c.IsBlocked);
}
