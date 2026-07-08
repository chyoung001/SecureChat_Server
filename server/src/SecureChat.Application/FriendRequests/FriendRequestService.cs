using Microsoft.Extensions.Logging;
using SecureChat.Application.Abstractions;
using SecureChat.Application.Common;
using SecureChat.Domain.Entities;
using SecureChat.Domain.Enums;

namespace SecureChat.Application.FriendRequests;

public class FriendRequestService : IFriendRequestService
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _notifier;
    private readonly ILogger<FriendRequestService> _logger;

    public FriendRequestService(IUnitOfWork uow, IRealtimeNotifier notifier,
        ILogger<FriendRequestService> logger)
    {
        _uow = uow;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<Result<FriendRequestDto>> SendAsync(
        Guid fromUserId, SendFriendRequestRequest request, CancellationToken ct = default)
    {
        if (fromUserId == request.ToUserId)
            return Result<FriendRequestDto>.Failure("Cannot send friend request to yourself");

        var toUser = await _uow.Users.FindByIdAsync(request.ToUserId, ct);
        if (toUser is null)
            return Result<FriendRequestDto>.Failure("User not found");

        // 양방향 모두 연락처인 경우에만 차단 (한쪽만 남아있는 불완전한 상태는 허용)
        var fwd = await _uow.Contacts.FindAsync(fromUserId, request.ToUserId, ct);
        var rev = await _uow.Contacts.FindAsync(request.ToUserId, fromUserId, ct);
        if (fwd is not null && rev is not null)
            return Result<FriendRequestDto>.Failure("Already a contact");

        var existingReq = await _uow.FriendRequests.FindPendingAsync(fromUserId, request.ToUserId, ct);
        if (existingReq is not null)
            return Result<FriendRequestDto>.Failure("Friend request already pending");

        var reverseReq = await _uow.FriendRequests.FindPendingAsync(request.ToUserId, fromUserId, ct);
        if (reverseReq is not null)
            return Result<FriendRequestDto>.Failure("This user already sent you a friend request");

        var fromUser = await _uow.Users.FindByIdAsync(fromUserId, ct);
        var friendReq = FriendRequest.Create(fromUserId, request.ToUserId, request.Message);
        await _uow.FriendRequests.AddAsync(friendReq, ct);
        await _uow.SaveChangesAsync(ct);

        var dto = ToDto(friendReq, fromUser!, toUser);
        await _notifier.SendToUserAsync(request.ToUserId, "RequestReceived", dto, ct);

        _logger.LogInformation("FriendRequest {RequestId} sent from {FromId} to {ToId}",
            friendReq.Id, fromUserId, request.ToUserId);

        return Result<FriendRequestDto>.Success(dto);
    }

    public async Task<Result<List<FriendRequestDto>>> GetIncomingAsync(
        Guid userId, CancellationToken ct = default)
    {
        var requests = await _uow.FriendRequests.GetIncomingPendingAsync(userId, ct);
        var dtos = requests.Select(r => ToDto(r, r.FromUser, r.ToUser)).ToList();
        return Result<List<FriendRequestDto>>.Success(dtos);
    }

    public async Task<Result<List<FriendRequestDto>>> GetOutgoingAsync(
        Guid userId, CancellationToken ct = default)
    {
        var requests = await _uow.FriendRequests.GetOutgoingPendingAsync(userId, ct);
        var dtos = requests.Select(r => ToDto(r, r.FromUser, r.ToUser)).ToList();
        return Result<List<FriendRequestDto>>.Success(dtos);
    }

    public async Task<Result<FriendRequestDto>> AcceptAsync(
        Guid callerId, Guid requestId, CancellationToken ct = default)
    {
        var req = await _uow.FriendRequests.FindByIdAsync(requestId, ct);
        if (req is null)
            return Result<FriendRequestDto>.Failure("Request not found");

        if (req.ToUserId != callerId)
            return Result<FriendRequestDto>.Failure("Not authorized");

        if (req.Status != FriendRequestStatus.Pending)
            return Result<FriendRequestDto>.Failure("Request is no longer pending");

        req.Accept();

        // 삭제 후 재수락 시 기존 Contact 행이 남아있을 수 있으므로 없을 때만 추가
        if (await _uow.Contacts.FindAsync(req.FromUserId, req.ToUserId, ct) is null)
            await _uow.Contacts.AddAsync(Contact.Create(req.FromUserId, req.ToUserId), ct);
        if (await _uow.Contacts.FindAsync(req.ToUserId, req.FromUserId, ct) is null)
            await _uow.Contacts.AddAsync(Contact.Create(req.ToUserId, req.FromUserId), ct);

        await _uow.SaveChangesAsync(ct);

        var dto = ToDto(req, req.FromUser, req.ToUser);
        await _notifier.SendToUserAsync(req.FromUserId, "RequestResponded", dto, ct);

        _logger.LogInformation("FriendRequest {RequestId} accepted by {CallerId}", requestId, callerId);
        return Result<FriendRequestDto>.Success(dto);
    }

    public async Task<Result<FriendRequestDto>> RejectAsync(
        Guid callerId, Guid requestId, CancellationToken ct = default)
    {
        var req = await _uow.FriendRequests.FindByIdAsync(requestId, ct);
        if (req is null)
            return Result<FriendRequestDto>.Failure("Request not found");

        if (req.ToUserId != callerId)
            return Result<FriendRequestDto>.Failure("Not authorized");

        if (req.Status != FriendRequestStatus.Pending)
            return Result<FriendRequestDto>.Failure("Request is no longer pending");

        req.Reject();
        await _uow.SaveChangesAsync(ct);

        var dto = ToDto(req, req.FromUser, req.ToUser);
        await _notifier.SendToUserAsync(req.FromUserId, "RequestResponded", dto, ct);

        _logger.LogInformation("FriendRequest {RequestId} rejected by {CallerId}", requestId, callerId);
        return Result<FriendRequestDto>.Success(dto);
    }

    private static FriendRequestDto ToDto(FriendRequest req, Domain.Entities.User from, Domain.Entities.User to) =>
        new(req.Id, req.FromUserId, from.Username, from.DisplayName,
            req.ToUserId, to.Username, to.DisplayName,
            req.Status.ToString(), req.Message, req.CreatedAt, req.RespondedAt);
}
