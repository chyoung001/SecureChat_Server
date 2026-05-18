using SecureChat.Common;
using SecureChat.Models;

namespace SecureChat.Services.Mock;

public class MockFriendRequestService : IFriendRequestService
{
    private readonly IAuthService    _authService;
    private readonly IContactService _contactService;

    // 내게 온 요청 (incoming)
    private readonly List<FriendRequest> _incoming = new()
    {
        new FriendRequest
        {
            RequestId      = "req-mock-1",
            SenderId       = "u-minjun",
            SenderUsername = "minjun_k",
            SenderName     = "김민준",
            ReceiverId     = "me",
            Status         = FriendRequestStatus.Pending,
            CreatedAt      = DateTime.UtcNow.AddMinutes(-5),
        },
    };

    // 내가 보낸 요청 (outgoing)
    private readonly List<FriendRequest> _outgoing = new();

    public event EventHandler<FriendRequest>? RequestReceived;
    public event EventHandler<FriendRequest>? RequestResponded;

    public MockFriendRequestService(IAuthService authService, IContactService contactService)
    {
        _authService    = authService;
        _contactService = contactService;
    }

    // ── 요청 전송 ──────────────────────────────────────────────
    public async Task SendRequestAsync(string targetUserId)
    {
        await Task.Delay(400); // 네트워크 시뮬레이션

        var me       = _authService.CurrentUser;
        var contacts = await _contactService.GetContactsAsync();
        var target   = contacts.FirstOrDefault(c => c.UserId == targetUserId);
        var receiverName = target?.DisplayName ?? target?.Username ?? targetUserId;

        var req = new FriendRequest
        {
            SenderId       = me?.UserId      ?? "me",
            SenderUsername = me?.Username    ?? "me",
            SenderName     = me?.DisplayName ?? "나",
            ReceiverId     = targetUserId,
            ReceiverName   = receiverName,
            Status         = FriendRequestStatus.Pending,
        };
        _outgoing.Add(req);

        // 5초 후 상대방이 수락하는 것을 시뮬레이션
        _ = SimulateResponseAsync(req, accept: true, delayMs: 5000);
    }

    // ── 수신 목록 ──────────────────────────────────────────────
    public Task<List<FriendRequest>> GetIncomingRequestsAsync()
        => Task.FromResult(_incoming.Where(r => r.Status == FriendRequestStatus.Pending).ToList());

    // ── 수락 ──────────────────────────────────────────────────
    public async Task AcceptRequestAsync(string requestId)
    {
        await Task.Delay(200);
        var req = _incoming.FirstOrDefault(r => r.RequestId == requestId);
        if (req == null) return;
        req.Status = FriendRequestStatus.Accepted;

        // 연락처에 자동 추가
        await _contactService.AddContactAsync(req.SenderId);
    }

    // ── 거절 ──────────────────────────────────────────────────
    public async Task RejectRequestAsync(string requestId)
    {
        await Task.Delay(200);
        var req = _incoming.FirstOrDefault(r => r.RequestId == requestId);
        if (req != null) req.Status = FriendRequestStatus.Rejected;
    }

    // ── 새 요청 수신 시뮬레이션 (테스트용) ────────────────────
    public void SimulateIncomingRequest(string senderName, string senderUsername)
    {
        var req = new FriendRequest
        {
            SenderId       = Guid.NewGuid().ToString(),
            SenderUsername = senderUsername,
            SenderName     = senderName,
            ReceiverId     = "me",
            Status         = FriendRequestStatus.Pending,
        };
        _incoming.Add(req);
        SyncContextHelper.Post(() => RequestReceived?.Invoke(this, req));
    }

    // ── 내가 보낸 요청에 대한 응답 시뮬레이션 ─────────────────
    private async Task SimulateResponseAsync(FriendRequest req, bool accept, int delayMs)
    {
        await Task.Delay(delayMs);

        req.Status = accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Rejected;

        if (accept)
            await _contactService.AddContactAsync(req.ReceiverId);

        SyncContextHelper.Post(() => RequestResponded?.Invoke(this, req));
    }
}
