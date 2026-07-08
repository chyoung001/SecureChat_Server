using Microsoft.Extensions.Logging;
using SecureChat.Application.Abstractions;
using SecureChat.Application.Common;
using SecureChat.Domain.Entities;

namespace SecureChat.Application.Messages;

public class MessageService : IMessageService
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _notifier;
    private readonly ILogger<MessageService> _logger;

    public MessageService(IUnitOfWork uow, IRealtimeNotifier notifier, ILogger<MessageService> logger)
    {
        _uow = uow;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<Result<Guid>> SendEncryptedMessageAsync(
        Guid callerId, SendMessageRequest request, CancellationToken ct = default)
    {
        // ① 호출자 멤버십 검증
        var memberIds = await _uow.Rooms.GetMemberIdsAsync(request.RoomId, ct);
        if (!memberIds.Contains(callerId))
            return Result<Guid>.Failure("Not a room member");

        // ② keys 배열 멤버십 검증 (외부인 끼워넣기 방지)
        var keyRecipients = request.Keys.Select(k => k.RecipientUserId).ToList();
        if (keyRecipients.Any(r => !memberIds.Contains(r)))
            return Result<Guid>.Failure("Invalid recipient in keys array");

        // ③ 중복 수신자 검증
        if (keyRecipients.Distinct().Count() != keyRecipients.Count)
            return Result<Guid>.Failure("Duplicate recipient in keys array");

        // ④ TtlSeconds 범위 검증 (0=영구, 5~604800)
        if (request.TtlSeconds != 0 && (request.TtlSeconds < 5 || request.TtlSeconds > 604800))
            return Result<Guid>.Failure("TtlSeconds must be 0 or between 5 and 604800");

        // ⑤ 본인 키 미포함 경고 (진행은 허용)
        if (!keyRecipients.Contains(callerId))
            _logger.LogWarning("Sender {UserId} did not include own key in room {RoomId}",
                callerId, request.RoomId);

        // ⑥ 저장 (트랜잭션 — SaveChanges 한 번으로 묶음)
        var message = Message.Create(
            request.RoomId, callerId,
            request.Iv, request.Ciphertext, request.HmacTag,
            request.TtlSeconds);

        await _uow.Messages.AddAsync(message, ct);

        foreach (var k in request.Keys)
            await _uow.Messages.AddKeyAsync(
                MessageKey.Create(message.Id, k.RecipientUserId, k.EncryptedAesKey), ct);

        var room = await _uow.Rooms.FindByIdAsync(request.RoomId, ct);
        room!.UpdateLastMessageAt(message.SentAt);

        await _uow.SaveChangesAsync(ct);

        // ⑦ 수신자별 개인화 브로드캐스트 (각자 본인 EncryptedAesKey만)
        foreach (var k in request.Keys)
        {
            var dto = new EncryptedMessageDto(
                message.Id, message.RoomId, message.SenderId,
                k.EncryptedAesKey,
                message.Iv, message.Ciphertext, message.HmacTag,
                message.TtlSeconds, message.SentAt, message.ExpiresAt);

            await _notifier.SendToUserAsync(k.RecipientUserId, "ReceiveEncryptedMessage", dto, ct);
        }

        _logger.LogInformation("Message {MessageId} sent to room {RoomId} by {UserId}",
            message.Id, request.RoomId, callerId);

        return Result<Guid>.Success(message.Id);
    }

    public async Task<Result<MessagePageDto>> GetPageAsync(
        Guid callerId, Guid roomId,
        DateTime? before, DateTime? after,
        int limit, CancellationToken ct = default)
    {
        if (!await _uow.Rooms.IsMemberAsync(roomId, callerId, ct))
            return Result<MessagePageDto>.Failure("Not a room member");

        var page = await _uow.Messages.GetPageWithKeyAsync(roomId, callerId, before, after, limit, ct);

        var items = page.Select(x => new EncryptedMessageDto(
            x.Message.Id, x.Message.RoomId, x.Message.SenderId,
            x.Key?.EncryptedAesKey ?? string.Empty,
            x.Message.Iv, x.Message.Ciphertext, x.Message.HmacTag,
            x.Message.TtlSeconds, x.Message.SentAt, x.Message.ExpiresAt
        )).ToList();

        DateTime? nextCursor = items.Count == limit ? items[^1].SentAt : null;

        return Result<MessagePageDto>.Success(new MessagePageDto(items, nextCursor));
    }

    public async Task<Result> AckDeliveryAsync(Guid callerId, Guid messageId, CancellationToken ct = default)
    {
        var message = await _uow.Messages.FindByIdAsync(messageId, ct);
        if (message is null)
            return Result.Failure("Message not found");

        var dto = new MessageStatusChangedDto(messageId, "Delivered");
        await _notifier.SendToUserAsync(message.SenderId, "MessageStatusChanged", dto, ct);

        return Result.Success();
    }

    public async Task<Result> MarkAsReadAsync(
        Guid callerId, Guid roomId, Guid lastReadMessageId, CancellationToken ct = default)
    {
        var member = await _uow.Rooms.GetMemberAsync(roomId, callerId, ct);
        if (member is null)
            return Result.Failure("Not a room member");

        // FK 오류 방지: 해당 messageId가 실제로 존재하는지 검증
        var message = await _uow.Messages.FindByIdAsync(lastReadMessageId, ct);
        if (message is null || message.RoomId != roomId)
            return Result.Failure("Message not found in this room");

        member.UpdateLastRead(lastReadMessageId, DateTime.UtcNow);
        await _uow.SaveChangesAsync(ct);

        var readAt = DateTime.UtcNow;
        var dto = new ReadReceiptDto(roomId, callerId, lastReadMessageId, readAt);
        await _notifier.SendToRoomAsync(roomId, "ReadReceipt", dto, ct);

        return Result.Success();
    }
}
