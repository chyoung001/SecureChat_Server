using SecureChat.Domain.Entities;

namespace SecureChat.Application.Messages;

public interface IMessageRepository
{
    Task AddAsync(Message message, CancellationToken ct = default);
    Task AddKeyAsync(MessageKey key, CancellationToken ct = default);
    Task<Message?> FindByIdAsync(Guid messageId, CancellationToken ct = default);
    Task<List<(Message Message, MessageKey? Key)>> GetPageWithKeyAsync(
        Guid roomId, Guid userId,
        DateTime? before, DateTime? after,
        int limit, CancellationToken ct = default);
    Task<(Message? Message, MessageKey? Key)> GetLastMessageWithKeyAsync(
        Guid roomId, Guid userId, CancellationToken ct = default);
    Task<int> CountAfterAsync(Guid roomId, DateTime afterSentAt, CancellationToken ct = default);
}
