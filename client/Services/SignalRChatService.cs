using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SecureChat.Common;
using SecureChat.Crypto;
using SecureChat.Models;
using SecureChat.Storage;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecureChat.Services;

public class SignalRChatService : IChatService, IAsyncDisposable
{
    public event EventHandler<ChatMessage>? MessageReceived;
    public event EventHandler<string>? MessageExpired;
    public event EventHandler<ConnectionState>? ConnectionStateChanged;
    public event EventHandler<string>? ScreenCaptureDetected;
    public event EventHandler<MessageStatusUpdate>? MessageStatusUpdated;
    public event EventHandler<ReadReceiptUpdate>? ReadReceiptReceived;
    // 내 메시지가 서버에서 확인됨: (localId, serverMessage). ChatPanel이 버블 ID를 교체하는 데 사용.
    public event EventHandler<(string LocalId, ChatMessage ServerMessage)>? OwnMessageConfirmed;
    public event EventHandler<FriendRequestNotification>? FriendRequestReceived;
    public event EventHandler<FriendRequestNotification>? FriendRequestResponded;
    public event EventHandler<string>? NewRoomReceived;
    public event EventHandler<string>? SessionInvalidated;
    public event EventHandler<string>? KickedFromRoom;
    public event EventHandler<string>? RoomMemberChanged;
    public event EventHandler<string>? AdminTransferred;

    public ConnectionState State => _state;

    private readonly IAuthService _authService;
    private readonly AppSettings _appSettings;
    private readonly LocalKeyStore _keyStore;
    private readonly IMessageStore _messageStore;
    private readonly ApiHttpClient _apiClient;
    private readonly ILogger<SignalRChatService> _logger;

    private HubConnection? _hub;
    private ConnectionState _state = ConnectionState.Disconnected;

    // D3: 공개키 캐시. (publicKeyPem, fingerprint) 쌍을 저장한다.
    private readonly Dictionary<string, CachedPublicKey> _publicKeyCache = new();
    private readonly object _cacheLock = new();

    // 방 멤버 표시 이름 캐시 (userId → displayName)
    private readonly Dictionary<string, string> _memberNameCache = new();
    private readonly object _nameCacheLock = new();

    // 낙관적 버블 ID 추적: roomId → 전송 순서대로 쌓인 로컬 UUID 큐
    private readonly Dictionary<string, Queue<string>> _pendingLocalIds = new();
    private readonly object _pendingLock = new();

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SignalRChatService(IAuthService authService, AppSettings appSettings,
        LocalKeyStore keyStore, IMessageStore messageStore, ApiHttpClient apiClient,
        ILogger<SignalRChatService> logger)
    {
        _authService = authService;
        _appSettings = appSettings;
        _keyStore = keyStore;
        _messageStore = messageStore;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task ConnectAsync()
    {
        if (_state == ConnectionState.Connected) return;

        SetState(ConnectionState.Connecting);

        var token = _authService.GetAccessToken()
            ?? throw new InvalidOperationException("Not authenticated");

        var hubUrl = $"{_appSettings.ServerUrl.TrimEnd('/')}{_appSettings.HubPath}";

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.HttpMessageHandlerFactory = _ =>
                {
                    var handler = new HttpClientHandler();
#if DEBUG
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif
                    return handler;
                };
            })
            .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        _hub.On<EncryptedMessageDto>("ReceiveEncryptedMessage", OnMessageReceived);
        _hub.On<MessageExpiredDto>("MessageExpired", OnMessageExpired);
        _hub.On<string, string>("PeerScreenCaptured", (roomId, _) =>
            SyncContextHelper.Post(() => ScreenCaptureDetected?.Invoke(this, roomId)));

        // D9: 송신자 측에서 Delivered 알림 수신.
        _hub.On<MessageStatusChangedDto>("MessageStatusChanged", dto =>
        {
            var status = Enum.TryParse<MessageStatus>(dto.Status, true, out var s)
                ? s : MessageStatus.Sent;
            SyncContextHelper.Post(() =>
                MessageStatusUpdated?.Invoke(this,
                    new MessageStatusUpdate(dto.MessageId.ToString(), status)));
        });

        // D9: 다른 참여자의 읽음 상태.
        _hub.On<ReadReceiptDto>("ReadReceipt", dto =>
        {
            SyncContextHelper.Post(() =>
                ReadReceiptReceived?.Invoke(this,
                    new ReadReceiptUpdate(
                        dto.RoomId.ToString(),
                        dto.UserId.ToString(),
                        dto.LastReadMessageId.ToString(),
                        dto.ReadAt)));
        });

        _hub.On<FriendRequestNotification>("RequestReceived", dto =>
            SyncContextHelper.Post(() => FriendRequestReceived?.Invoke(this, dto)));

        _hub.On<FriendRequestNotification>("RequestResponded", dto =>
            SyncContextHelper.Post(() => FriendRequestResponded?.Invoke(this, dto)));

        _hub.On<RoomInvitedDto>("RoomInvited", dto =>
            SyncContextHelper.Post(() => NewRoomReceived?.Invoke(this, dto.Id.ToString())));

        _hub.On<SessionInvalidatedDto>("SessionInvalidated", dto =>
            SyncContextHelper.Post(() => SessionInvalidated?.Invoke(this, dto.Reason)));

        _hub.On<KickedFromRoomDto>("KickedFromRoom", dto =>
            SyncContextHelper.Post(() => KickedFromRoom?.Invoke(this, dto.RoomId.ToString())));

        _hub.On<RoomMemberChangedDto>("UserJoinedRoom", dto =>
            SyncContextHelper.Post(() => RoomMemberChanged?.Invoke(this, dto.RoomId.ToString())));

        _hub.On<RoomMemberChangedDto>("UserLeftRoom", dto =>
            SyncContextHelper.Post(() => RoomMemberChanged?.Invoke(this, dto.RoomId.ToString())));

        _hub.On<RoomMemberChangedDto>("AdminTransferred", dto =>
            SyncContextHelper.Post(() => AdminTransferred?.Invoke(this, dto.RoomId.ToString())));

        _hub.Reconnecting += _ => { SetState(ConnectionState.Connecting); return Task.CompletedTask; };
        _hub.Reconnected += _ => { SetState(ConnectionState.Connected); return Task.CompletedTask; };
        _hub.Closed += _ => { SetState(ConnectionState.Disconnected); return Task.CompletedTask; };

        try
        {
            await _hub.StartAsync();
            SetState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR connection failed");
            SetState(ConnectionState.Disconnected);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hub is not null)
        {
            await _hub.StopAsync();
            SetState(ConnectionState.Disconnected);
        }
    }

    public async Task SendMessageAsync(string roomId, string plainText, int ttlSeconds = 0, string? localMessageId = null)
    {
        if (_hub is null || _state != ConnectionState.Connected)
            throw new InvalidOperationException("Not connected");

        var roomGuid = Guid.Parse(roomId);
        var members = await FetchRoomMembersAsync(roomGuid);
        if (members.Count == 0)
            throw new InvalidOperationException("Could not load room members");

        var (ciphertextB64, ivB64, tagB64, aesKey) = E2ECrypto.EncryptMessage(plainText);

        // D11: 모든 멤버에 대해 공개키 확보가 필수. 한 명이라도 누락되면 전송 중단.
        var keys = new List<object>();
        var missing = new List<Guid>();
        foreach (var memberId in members)
        {
            var pubKey = await GetPublicKeyAsync(memberId);
            if (pubKey is null)
            {
                missing.Add(memberId);
                continue;
            }
            keys.Add(new
            {
                recipientUserId = memberId,
                encryptedAesKey = E2ECrypto.EncryptAesKey(aesKey, pubKey)
            });
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"공개키가 등록되지 않은 멤버가 있어 전송할 수 없습니다 ({missing.Count}명). 잠시 후 다시 시도하세요.");
        }

        // 낙관적 로컬 UUID를 큐에 등록 — 서버 에코 도착 시 교체에 사용
        if (localMessageId is not null)
        {
            lock (_pendingLock)
            {
                if (!_pendingLocalIds.TryGetValue(roomId, out var q))
                    _pendingLocalIds[roomId] = q = new Queue<string>();
                q.Enqueue(localMessageId);
            }
        }

        await _hub.InvokeAsync("SendEncryptedMessage", new
        {
            roomId = roomGuid,
            iv = ivB64,
            ciphertext = ciphertextB64,
            hmacTag = tagB64,
            ttlSeconds,
            keys
        });
    }

    public async Task JoinRoomAsync(Guid roomId)
    {
        if (_hub is null || _state != ConnectionState.Connected) return;
        try { await _hub.InvokeAsync("JoinRoom", roomId); }
        catch (Exception ex) { _logger.LogWarning(ex, "JoinRoom failed for {RoomId}", roomId); }
    }

    public async Task LeaveRoomHubAsync(Guid roomId)
    {
        if (_hub is null || _state != ConnectionState.Connected) return;
        try { await _hub.InvokeAsync("LeaveRoom", roomId); }
        catch (Exception ex) { _logger.LogWarning(ex, "LeaveRoom failed for {RoomId}", roomId); }
    }

    // D10: 읽음 처리. 채팅창이 활성 상태이거나 마지막 메시지가 보이는 순간 호출.
    public async Task MarkAsReadAsync(Guid roomId, Guid lastReadMessageId)
    {
        if (_hub is null || _state != ConnectionState.Connected) return;
        try { await _hub.InvokeAsync("MarkAsRead", roomId, lastReadMessageId); }
        catch (Exception ex) { _logger.LogWarning(ex, "MarkAsRead failed"); }
    }

    public async Task NotifyScreenCaptureAttemptAsync(string roomId)
    {
        if (_hub is null || _state != ConnectionState.Connected) return;
        try { await _hub.InvokeAsync("NotifyScreenCapture", Guid.Parse(roomId)); }
        catch (Exception ex) { _logger.LogWarning(ex, "NotifyScreenCapture failed (non-fatal)"); }
    }

    // D3: 상대 키가 회전되었을 때 호출. 다음 GetPublicKeyAsync에서 재조회한다.
    public void InvalidatePublicKeyCache(Guid userId)
    {
        lock (_cacheLock) _publicKeyCache.Remove(userId.ToString());
    }

    // D13: 방 진입 시 서버에서 메시지 히스토리를 가져와 복호화 후 store에 적재.
    // 가장 최근 N개부터 가져온다 (before=null 이면 latest).
    public async Task<int> LoadHistoryAsync(Guid roomId, int limit = 50)
    {
        var keys = _keyStore.LoadKeyPair();
        if (keys is null) return 0;

        // 방 진입마다 멤버 목록을 로딩해 이름 캐시 갱신 (방마다 멤버가 다름)
        await FetchRoomMembersAsync(roomId);

        try
        {
            var url = $"/api/rooms/{roomId}/messages?limit={limit}";
            var page = await _apiClient.Client.GetFromJsonAsync<MessagePageResponse>(url, _json);
            if (page?.Items is null) return 0;

            var myId = _authService.CurrentUser?.UserId;
            int loaded = 0;

            // 서버가 SentAt DESC로 반환하므로 역순으로 삽입 → 오래된 것부터 store에 쌓임.
            // OrderBy(SentAt) 대신 Reverse()를 써야 동일 SentAt 메시지의 서버 순서가 보존된다.
            foreach (var dto in ((IEnumerable<EncryptedMessageDto>)page.Items).Reverse())
            {
                string plain;
                try
                {
                    var aes = E2ECrypto.DecryptAesKey(dto.EncryptedAesKey, keys.Value.privatePem);
                    plain = E2ECrypto.DecryptMessage(dto.Ciphertext, dto.Iv, dto.HmacTag, aes);
                }
                catch
                {
                    plain = "⚠️ 복호화 실패";
                }

                var msg = new ChatMessage
                {
                    MessageId  = dto.Id.ToString(),
                    RoomId     = dto.RoomId.ToString(),
                    SenderId   = dto.SenderId.ToString(),
                    SenderName = ResolveSenderName(dto.SenderId.ToString()),
                    PlainText  = plain,
                    SentAt     = dto.SentAt,
                    TtlSeconds = dto.TtlSeconds,
                    ExpiresAt  = dto.ExpiresAt == DateTime.MaxValue ? null : dto.ExpiresAt,
                    Status     = MessageStatus.Delivered,
                    IsMine     = dto.SenderId.ToString() == myId
                };

                _messageStore.AddMessage(msg); // 중복 방지는 store가 담당
                loaded++;
            }
            return loaded;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LoadHistory failed for {RoomId}", roomId);
            return 0;
        }
    }

    private class MessagePageResponse
    {
        public List<EncryptedMessageDto> Items { get; set; } = new();
        public DateTime? NextCursor { get; set; }
    }

    // ── 수신 핸들러 ──────────────────────────────────────────────────────────

    private void OnMessageReceived(EncryptedMessageDto dto)
    {
        var keys = _keyStore.LoadKeyPair();
        if (keys is null)
        {
            _logger.LogWarning("No local key pair; cannot decrypt message {Id}", dto.Id);
            return;
        }

        string plainText;
        try
        {
            var aesKey = E2ECrypto.DecryptAesKey(dto.EncryptedAesKey, keys.Value.privatePem);
            plainText = E2ECrypto.DecryptMessage(dto.Ciphertext, dto.Iv, dto.HmacTag, aesKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed for message {Id}", dto.Id);
            plainText = "⚠️ 복호화 실패";
        }

        var myId = _authService.CurrentUser?.UserId;
        var isMine = dto.SenderId.ToString() == myId;
        var roomId = dto.RoomId.ToString();
        var serverId = dto.Id.ToString();

        var msg = new ChatMessage
        {
            MessageId  = serverId,
            RoomId     = roomId,
            SenderId   = dto.SenderId.ToString(),
            SenderName = ResolveSenderName(dto.SenderId.ToString()),
            PlainText  = plainText,
            SentAt     = dto.SentAt,
            TtlSeconds = dto.TtlSeconds,
            ExpiresAt  = dto.ExpiresAt == DateTime.MaxValue ? null : dto.ExpiresAt,
            Status     = MessageStatus.Delivered,
            IsMine     = isMine
        };

        if (isMine)
        {
            // 낙관적 버블의 로컬 UUID를 서버 UUID로 교체.
            // 큐에 없으면 다른 디바이스 발송이므로 그냥 store에 추가.
            string? localId = null;
            lock (_pendingLock)
            {
                if (_pendingLocalIds.TryGetValue(roomId, out var q) && q.Count > 0)
                    localId = q.Dequeue();
            }

            if (localId is not null)
            {
                // store의 로컬 UUID → 서버 UUID 교체, 버블 딕셔너리 교체는 ChatPanel이 처리
                _messageStore.ReplaceMessageId(roomId, localId, serverId);
                SyncContextHelper.Post(() =>
                    OwnMessageConfirmed?.Invoke(this, (localId, msg)));
            }
            else
            {
                // 다른 디바이스 발송 or 큐 미스 — 새 메시지로 추가
                _messageStore.AddMessage(msg);
            }
        }
        else
        {
            _messageStore.AddMessage(msg);
            SyncContextHelper.Post(() => MessageReceived?.Invoke(this, msg));

            _ = Task.Run(async () =>
            {
                try { await _hub!.InvokeAsync("AckDelivery", dto.Id); }
                catch { /* non-fatal */ }
            });
        }
    }

    private void OnMessageExpired(MessageExpiredDto dto)
    {
        foreach (var id in dto.MessageIds)
            SyncContextHelper.Post(() => MessageExpired?.Invoke(this, id.ToString()));
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────────────

    private async Task<List<Guid>> FetchRoomMembersAsync(Guid roomId)
    {
        try
        {
            var room = await _apiClient.Client.GetFromJsonAsync<RoomDetailDto>(
                $"/api/rooms/{roomId}", _json);
            if (room is null) return new();

            lock (_nameCacheLock)
                foreach (var m in room.Members)
                    _memberNameCache[m.UserId.ToString()] = m.DisplayLabel;

            return room.Members.Select(m => m.UserId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FetchRoomMembers failed for {RoomId}", roomId);
            return new();
        }
    }

    private string ResolveSenderName(string userId)
    {
        lock (_nameCacheLock)
        {
            if (_memberNameCache.TryGetValue(userId, out var name)) return name;
        }
        // 캐시 미스 — 백그라운드에서 멤버 정보를 가져와 캐시 채움 (다음 메시지부터 반영)
        _ = Task.Run(async () =>
        {
            try
            {
                var dto = await _apiClient.Client.GetFromJsonAsync<UserInfoDto>(
                    $"/api/users/{userId}", _json);
                if (dto is null) return;
                var label = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.Username : dto.DisplayName;
                lock (_nameCacheLock) _memberNameCache[userId] = label;
            }
            catch { /* non-fatal */ }
        });
        // 이번 메시지는 앞 8자로 임시 표시
        return userId.Length >= 8 ? userId[..8] : userId;
    }

    private async Task<string?> GetPublicKeyAsync(Guid userId)
    {
        var key = userId.ToString();
        lock (_cacheLock)
        {
            if (_publicKeyCache.TryGetValue(key, out var cached))
                return cached.PublicKeyPem;
        }

        try
        {
            var dto = await _apiClient.Client.GetFromJsonAsync<PublicKeyDto>(
                $"/api/users/{userId}/public-key", _json);
            if (dto is null) return null;
            lock (_cacheLock)
            {
                _publicKeyCache[key] = new CachedPublicKey(dto.PublicKeyPem, dto.KeyFingerprint);
            }
            return dto.PublicKeyPem;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetPublicKey failed for {UserId}", userId);
            return null;
        }
    }

    private void SetState(ConnectionState newState)
    {
        _state = newState;
        SyncContextHelper.Post(() => ConnectionStateChanged?.Invoke(this, newState));
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }

    // ── 로컬 DTO ─────────────────────────────────────────────────────────────
    private record CachedPublicKey(string PublicKeyPem, string Fingerprint);
    private record RoomInvitedDto(Guid Id);
    private record SessionInvalidatedDto(string Reason);
    private record KickedFromRoomDto(Guid RoomId);
    private record RoomMemberChangedDto(Guid RoomId);
    private record UserInfoDto(Guid Id, string Username, string DisplayName);

    internal class EncryptedMessageDto
    {
        public Guid     Id             { get; set; }
        public Guid     RoomId         { get; set; }
        public Guid     SenderId       { get; set; }
        public string   EncryptedAesKey{ get; set; } = "";
        public string   Iv             { get; set; } = "";
        public string   Ciphertext     { get; set; } = "";
        public string   HmacTag        { get; set; } = "";
        public int      TtlSeconds     { get; set; }
        public DateTime SentAt         { get; set; }
        public DateTime ExpiresAt      { get; set; }
    }

    internal class MessageExpiredDto
    {
        public List<Guid> MessageIds { get; set; } = new();
        public Guid       RoomId     { get; set; }
    }

    private class MessageStatusChangedDto
    {
        public Guid   MessageId { get; set; }
        public string Status    { get; set; } = "";
    }

    private class ReadReceiptDto
    {
        public Guid     RoomId            { get; set; }
        public Guid     UserId            { get; set; }
        public Guid     LastReadMessageId { get; set; }
        public DateTime ReadAt            { get; set; }
    }

    private class RoomDetailDto
    {
        public Guid               Id      { get; set; }
        public List<RoomMemberDto> Members { get; set; } = new();
    }
    private class RoomMemberDto
    {
        public Guid   UserId      { get; set; }
        public string Username    { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string DisplayLabel => string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName;
    }
    private class PublicKeyDto
    {
        public Guid   UserId         { get; set; }
        public string PublicKeyPem   { get; set; } = "";
        public string KeyFingerprint { get; set; } = "";
    }
}

public record MessageStatusUpdate(string MessageId, MessageStatus Status);
public record ReadReceiptUpdate(string RoomId, string UserId, string LastReadMessageId, DateTime ReadAt);
