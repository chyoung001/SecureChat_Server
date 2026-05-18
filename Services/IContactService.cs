using SecureChat.Models;

namespace SecureChat.Services;

public interface IContactService
{
    Task<List<Contact>> GetContactsAsync();
    Task<Contact?> SearchUserAsync(string username);
    Task AddContactAsync(string userId);
    Task<Contact?> GetContactPublicKeyAsync(string userId);
    Task MarkVerifiedAsync(string userId);
    Task RemoveContactAsync(string userId);
    Task BlockContactAsync(string userId);
}
