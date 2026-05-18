using SecureChat.Models;

namespace SecureChat.Services;

public class InMemoryMessageStore : IMessageStore
{
    private readonly Dictionary<string, List<ChatMessage>> _messagesByRoom = new();
    private readonly object _lock = new();
    private long _seq = 0;

    public event EventHandler<ChatMessage>? MessageAdded;
    public event EventHandler<string>? MessageRemoved;

    public void AddMessage(ChatMessage message)
    {
        bool added;
        lock (_lock)
        {
            if (!_messagesByRoom.TryGetValue(message.RoomId, out var list))
            {
                list = new List<ChatMessage>();
                _messagesByRoom[message.RoomId] = list;
            }

            // 동일 messageId가 이미 있으면 무시 (낙관적 UI + Hub 에코 + 페이지네이션 중복 방지).
            if (list.Any(m => m.MessageId == message.MessageId))
                added = false;
            else
            {
                message.StoreSeq = ++_seq;
                list.Add(message);
                added = true;
            }
        }
        if (added) MessageAdded?.Invoke(this, message);
    }

    public List<ChatMessage> GetMessagesForRoom(string roomId)
    {
        lock (_lock)
        {
            return _messagesByRoom.TryGetValue(roomId, out var list)
                ? new List<ChatMessage>(list)
                : new List<ChatMessage>();
        }
    }

    public void RemoveExpiredMessages()
    {
        var now = DateTime.UtcNow;
        var expired = new List<string>();
        lock (_lock)
        {
            foreach (var list in _messagesByRoom.Values)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var m = list[i];
                    if (m.ExpiresAt.HasValue && m.ExpiresAt.Value <= now)
                    {
                        expired.Add(m.MessageId);
                        list.RemoveAt(i);
                    }
                }
            }
        }
        foreach (var id in expired)
        {
            MessageRemoved?.Invoke(this, id);
        }
    }

    public void RemoveMessage(string messageId)
    {
        bool removed = false;
        lock (_lock)
        {
            foreach (var list in _messagesByRoom.Values)
            {
                var idx = list.FindIndex(m => m.MessageId == messageId);
                if (idx >= 0)
                {
                    list.RemoveAt(idx);
                    removed = true;
                    break;
                }
            }
        }
        if (removed)
            MessageRemoved?.Invoke(this, messageId);
    }

    public bool ReplaceMessageId(string roomId, string localId, string serverId)
    {
        lock (_lock)
        {
            if (!_messagesByRoom.TryGetValue(roomId, out var list)) return false;
            var msg = list.FirstOrDefault(m => m.MessageId == localId);
            if (msg is null) return false;
            msg.MessageId = serverId;
            return true;
        }
    }
}
