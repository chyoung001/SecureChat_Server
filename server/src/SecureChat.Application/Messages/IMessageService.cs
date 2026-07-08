using SecureChat.Application.Common;

namespace SecureChat.Application.Messages;

public interface IMessageService
{
    Task<Result<Guid>> SendEncryptedMessageAsync(
        Guid callerId, SendMessageRequest request, CancellationToken ct = default);

    Task<Result<MessagePageDto>> GetPageAsync(
        Guid callerId, Guid roomId,
        DateTime? before, DateTime? after,
        int limit, CancellationToken ct = default);

    Task<Result> AckDeliveryAsync(Guid callerId, Guid messageId, CancellationToken ct = default);

    Task<Result> MarkAsReadAsync(
        Guid callerId, Guid roomId, Guid lastReadMessageId, CancellationToken ct = default);
}
