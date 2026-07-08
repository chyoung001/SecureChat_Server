using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SecureChat.Models;

namespace SecureChat.Services;

public class HttpRoomService : IRoomService
{
    private readonly ApiHttpClient _apiClient;
    private readonly IChatService _chatService;
    private readonly IAuthService _authService;
    private readonly ILogger<HttpRoomService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HttpRoomService(ApiHttpClient apiClient, IChatService chatService,
        IAuthService authService, ILogger<HttpRoomService> logger)
    {
        _apiClient   = apiClient;
        _chatService = chatService;
        _authService = authService;
        _logger      = logger;
    }

    public async Task<List<ChatRoom>> GetMyRoomsAsync()
    {
        try
        {
            var myId = _authService.CurrentUser?.UserId;
            var dtos = await _apiClient.Client.GetFromJsonAsync<List<RoomSummaryDto>>("/api/rooms", _json);
            return dtos?.Select(d => ToModel(d, myId)).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMyRooms failed");
            return new();
        }
    }

    public async Task<ChatRoom?> GetRoomAsync(string roomId)
    {
        try
        {
            var myId = _authService.CurrentUser?.UserId;
            var dto = await _apiClient.Client.GetFromJsonAsync<RoomDetailDto>(
                $"/api/rooms/{roomId}", _json);
            return dto is null ? null : ToModelFromDetail(dto, myId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRoom failed for {RoomId}", roomId);
            return null;
        }
    }

    public async Task<ChatRoom> CreateRoomAsync(string name, List<string> memberIds)
    {
        var myId = _authService.CurrentUser?.UserId;
        var response = await _apiClient.Client.PostAsJsonAsync("/api/rooms", new
        {
            name,
            memberIds = memberIds.Select(Guid.Parse).ToList()
        });
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<RoomDetailDto>(_json)
            ?? throw new InvalidOperationException("Empty response");
        return ToModelFromDetail(dto, myId);
    }

    public async Task<ChatRoom> CreateDirectMessageAsync(string targetUserId)
    {
        var myId = _authService.CurrentUser?.UserId;
        var response = await _apiClient.Client.PostAsJsonAsync("/api/rooms/direct", new
        {
            otherUserId = Guid.Parse(targetUserId)
        });
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<RoomDetailDto>(_json)
            ?? throw new InvalidOperationException("Empty response");
        return ToModelFromDetail(dto, myId);
    }

    public async Task JoinRoomAsync(string roomId)
    {
        if (_chatService is SignalRChatService signalR)
            await signalR.JoinRoomAsync(Guid.Parse(roomId));
    }

    public async Task<bool> InviteMemberAsync(string roomId, string targetUserId)
    {
        try
        {
            var resp = await _apiClient.Client.PostAsJsonAsync(
                $"/api/rooms/{roomId}/invite",
                new { userId = Guid.Parse(targetUserId) });
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InviteMember failed for room {RoomId}", roomId);
            return false;
        }
    }

    public async Task<bool> TransferAdminAsync(string roomId, string newAdminUserId)
    {
        try
        {
            var resp = await _apiClient.Client.PostAsJsonAsync(
                $"/api/rooms/{roomId}/transfer-admin",
                new { newAdminUserId = Guid.Parse(newAdminUserId) });
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TransferAdmin failed for room {RoomId}", roomId);
            return false;
        }
    }

    public async Task<bool> KickMemberAsync(string roomId, string targetUserId)
    {
        try
        {
            var resp = await _apiClient.Client.DeleteAsync(
                $"/api/rooms/{roomId}/members/{targetUserId}");
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KickMember failed for room {RoomId}", roomId);
            return false;
        }
    }

    public async Task LeaveRoomAsync(string roomId)
    {
        try
        {
            await _apiClient.Client.DeleteAsync($"/api/rooms/{roomId}/leave");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LeaveRoom failed for {RoomId}", roomId);
        }
    }

    private static ChatRoom ToModel(RoomSummaryDto dto, string? myId) => new()
    {
        RoomId          = dto.Id.ToString(),
        Name            = dto.Name ?? (dto.IsDirect
            ? dto.Members.FirstOrDefault(m => m.Id.ToString() != myId)?.DisplayName
              ?? dto.Members.FirstOrDefault()?.DisplayName ?? "DM"
            : "방"),
        IsDirectMessage = dto.IsDirect,
        MemberIds       = dto.Members.Select(m => m.Id.ToString()).ToList(),
        Members         = dto.Members.Select(m => new Models.RoomMember
        {
            UserId      = m.Id.ToString(),
            DisplayName = string.IsNullOrWhiteSpace(m.DisplayName) ? m.Username : m.DisplayName,
            IsAdmin     = false
        }).ToList(),
        LastActivityAt  = dto.LastMessageAt ?? DateTime.MinValue,
        UnreadCount     = dto.UnreadCount,
        LastMessageText = dto.LastMessage?.Ciphertext is not null ? "🔒 암호화된 메시지" : null
    };

    private static ChatRoom ToModelFromDetail(RoomDetailDto dto, string? myId) => new()
    {
        RoomId          = dto.Id.ToString(),
        Name            = dto.Name ?? (dto.IsDirect
            ? dto.Members.FirstOrDefault(m => m.UserId.ToString() != myId)?.DisplayName
              ?? dto.Members.FirstOrDefault()?.DisplayName ?? "DM"
            : "방"),
        IsDirectMessage = dto.IsDirect,
        MemberIds       = dto.Members.Select(m => m.UserId.ToString()).ToList(),
        Members         = dto.Members.Select(m => new Models.RoomMember
        {
            UserId      = m.UserId.ToString(),
            DisplayName = string.IsNullOrWhiteSpace(m.DisplayName) ? m.Username : m.DisplayName,
            IsAdmin     = m.IsAdmin,
            LastSeenAt  = m.LastSeenAt
        }).ToList(),
        LastActivityAt  = DateTime.UtcNow,
        UnreadCount     = 0
    };

    private record UserSummaryDto(Guid Id, string Username, string DisplayName);
    private record LastMessageDto(string Ciphertext);
    private record RoomSummaryDto(Guid Id, string? Name, bool IsDirect,
        List<UserSummaryDto> Members, LastMessageDto? LastMessage,
        int UnreadCount, DateTime? LastMessageAt);
    private record RoomMemberDto(Guid UserId, string Username, string DisplayName, bool IsAdmin,
        DateTime LastSeenAt);
    private record RoomDetailDto(Guid Id, string? Name, bool IsDirect,
        Guid CreatedById, DateTime CreatedAt, List<RoomMemberDto> Members);
}
