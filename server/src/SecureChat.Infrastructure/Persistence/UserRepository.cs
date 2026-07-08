using Microsoft.EntityFrameworkCore;
using SecureChat.Application.Users;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FindAsync([id], ct).AsTask();

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);

    public Task<List<User>> SearchByUsernameAsync(string prefix, int limit, CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Username.StartsWith(prefix))
            .OrderBy(u => u.Username)
            .Take(limit)
            .ToListAsync(ct);
}
