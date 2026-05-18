using SecureChat.Domain.Entities;

namespace SecureChat.Application.Users;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<List<User>> SearchByUsernameAsync(string prefix, int limit, CancellationToken ct = default);
}
