using SecureChat.Models;

namespace SecureChat.Services;

public interface IFriendRequestService
{
    // 상대방에게 친구 요청 전송
    Task SendRequestAsync(string targetUserId);

    // 내게 온 대기 중인 요청 목록
    Task<List<FriendRequest>> GetIncomingRequestsAsync();

    // 수락 / 거절
    Task AcceptRequestAsync(string requestId);
    Task RejectRequestAsync(string requestId);

    // 상대방이 보낸 요청이 도착했을 때 (UI에서 배지 갱신용)
    event EventHandler<FriendRequest>? RequestReceived;

    // 내가 보낸 요청에 대한 상대방 응답이 도착했을 때
    event EventHandler<FriendRequest>? RequestResponded;
}
