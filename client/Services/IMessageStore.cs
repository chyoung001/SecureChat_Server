using SecureChat.Models;

namespace SecureChat.Services;

public interface IMessageStore
{
    void AddMessage(ChatMessage message);
    List<ChatMessage> GetMessagesForRoom(string roomId);
    void RemoveExpiredMessages();
    void RemoveMessage(string messageId);
    // 낙관적 로컬 UUID를 서버 발급 UUID로 교체. 해당 roomId에서 가장 최근 IsMine 메시지 중 일치하는 것을 찾는다.
    bool ReplaceMessageId(string roomId, string localId, string serverId);
    event EventHandler<ChatMessage>? MessageAdded;
    event EventHandler<string>? MessageRemoved;
}
