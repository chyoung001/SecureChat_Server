using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureChat.Application.Abstractions;
using SecureChat.Application.Messages;
using SecureChat.Infrastructure.Persistence;

namespace SecureChat.Infrastructure.BackgroundJobs;

public class MessageExpirationWorker : BackgroundService
{
    private const int BatchSize = 500;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageExpirationWorker> _logger;

    public MessageExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<MessageExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageExpirationWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            await ProcessBatchAsync(stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();

            var now = DateTime.UtcNow;

            // 만료된 메시지 조회 (DateTime.MaxValue = 영구 메시지 제외)
            // 오래된 만료 메시지부터 FIFO로 처리 — OrderBy 없이 Take를 쓰면
            // 같은 메시지가 매 사이클 반복 선택되어 미처리될 수 있음
            var expired = await db.Messages
                .Where(m => m.ExpiresAt < now && m.ExpiresAt < DateTime.MaxValue)
                .OrderBy(m => m.ExpiresAt)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (expired.Count == 0) return;

            // RoomId별 그룹화하여 배치 이벤트 발송
            var byRoom = expired.GroupBy(m => m.RoomId);

            foreach (var group in byRoom)
            {
                var msgIds = group.Select(m => m.Id).ToList();

                // LastReadMessageId 참조 무효화
                await db.RoomMembers
                    .Where(rm => rm.LastReadMessageId.HasValue
                                 && msgIds.Contains(rm.LastReadMessageId.Value))
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(rm => rm.LastReadMessageId, (Guid?)null), ct);

                // MessageKey 삭제 후 Message 삭제
                await db.MessageKeys
                    .Where(mk => msgIds.Contains(mk.MessageId))
                    .ExecuteDeleteAsync(ct);

                await db.Messages
                    .Where(m => msgIds.Contains(m.Id))
                    .ExecuteDeleteAsync(ct);

                // 클라이언트 알림
                var dto = new MessageExpiredDto(msgIds, group.Key);
                await notifier.SendToRoomAsync(group.Key, "MessageExpired", dto, ct);

                _logger.LogInformation(
                    "Expired {Count} messages in room {RoomId}", msgIds.Count, group.Key);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error in MessageExpirationWorker");
        }
    }
}
