using Microsoft.EntityFrameworkCore;
using SecureChat.Application.Messages;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Persistence;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _db;

    public MessageRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Message message, CancellationToken ct = default) =>
        await _db.Messages.AddAsync(message, ct);

    public async Task AddKeyAsync(MessageKey key, CancellationToken ct = default) =>
        await _db.MessageKeys.AddAsync(key, ct);

    public Task<Message?> FindByIdAsync(Guid messageId, CancellationToken ct = default) =>
        _db.Messages.FindAsync([messageId], ct).AsTask();

    public async Task<List<(Message Message, MessageKey? Key)>> GetPageWithKeyAsync(
        Guid roomId, Guid userId,
        DateTime? before, DateTime? after,
        int limit, CancellationToken ct = default)
    {
        IQueryable<Message> query = _db.Messages
            .Where(m => m.RoomId == roomId && m.ExpiresAt > DateTime.UtcNow);

        query = before.HasValue
            ? query.Where(m => m.SentAt < before.Value).OrderByDescending(m => m.SentAt)
            : after.HasValue
                ? query.Where(m => m.SentAt > after.Value).OrderBy(m => m.SentAt)
                : query.OrderByDescending(m => m.SentAt);

        var messages = await query.Take(limit).ToListAsync(ct);
        if (messages.Count == 0) return [];

        var messageIds = messages.Select(m => m.Id).ToList();
        var keys = await _db.MessageKeys
            .Where(k => messageIds.Contains(k.MessageId) && k.RecipientUserId == userId)
            .ToListAsync(ct);

        var keyMap = keys.ToDictionary(k => k.MessageId);
        return messages
            .Select(m => (m, keyMap.TryGetValue(m.Id, out var k) ? k : (MessageKey?)null))
            .ToList();
    }

    public async Task<(Message? Message, MessageKey? Key)> GetLastMessageWithKeyAsync(
        Guid roomId, Guid userId, CancellationToken ct = default)
    {
        var msg = await _db.Messages
            .Where(m => m.RoomId == roomId && m.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefaultAsync(ct);

        if (msg is null) return (null, null);

        var key = await _db.MessageKeys
            .FirstOrDefaultAsync(k => k.MessageId == msg.Id && k.RecipientUserId == userId, ct);

        return (msg, key);
    }

    public Task<int> CountAfterAsync(Guid roomId, DateTime afterSentAt, CancellationToken ct = default) =>
        _db.Messages.CountAsync(
            m => m.RoomId == roomId
              && m.SentAt > afterSentAt
              && m.ExpiresAt > DateTime.UtcNow, ct);
}
