using Microsoft.Extensions.Logging;
using SecureChat.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecureChat.Services;

public class HttpFriendRequestService : IFriendRequestService, IDisposable
{
    private readonly ApiHttpClient _apiClient;
    private readonly IChatService _chatService;
    private readonly ILogger<HttpFriendRequestService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public event EventHandler<FriendRequest>? RequestReceived;
    public event EventHandler<FriendRequest>? RequestResponded;

    public HttpFriendRequestService(ApiHttpClient apiClient, IChatService chatService,
        ILogger<HttpFriendRequestService> logger)
    {
        _apiClient = apiClient;
        _chatService = chatService;
        _logger = logger;

        _chatService.FriendRequestReceived  += OnRequestReceived;
        _chatService.FriendRequestResponded += OnRequestResponded;
    }

    public async Task SendRequestAsync(string targetUserId)
    {
        try
        {
            var resp = await _apiClient.Client.PostAsJsonAsync(
                "/api/friend-requests",
                new { toUserId = Guid.Parse(targetUserId) });

            if (!resp.IsSuccessStatusCode)
                _logger.LogWarning("SendFriendRequest failed: {Status}", resp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendFriendRequest exception");
        }
    }

    public async Task<List<FriendRequest>> GetIncomingRequestsAsync()
    {
        try
        {
            var dtos = await _apiClient.Client
                .GetFromJsonAsync<List<FriendRequestDto>>("/api/friend-requests/incoming", _json);
            return dtos?.Select(ToModel).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetIncomingRequests exception");
            return new();
        }
    }

    public async Task AcceptRequestAsync(string requestId)
    {
        try
        {
            var resp = await _apiClient.Client
                .PostAsync($"/api/friend-requests/{requestId}/accept", null);
            if (!resp.IsSuccessStatusCode)
                _logger.LogWarning("AcceptFriendRequest failed: {Status}", resp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AcceptFriendRequest exception");
        }
    }

    public async Task RejectRequestAsync(string requestId)
    {
        try
        {
            var resp = await _apiClient.Client
                .PostAsync($"/api/friend-requests/{requestId}/reject", null);
            if (!resp.IsSuccessStatusCode)
                _logger.LogWarning("RejectFriendRequest failed: {Status}", resp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RejectFriendRequest exception");
        }
    }

    public void Dispose()
    {
        _chatService.FriendRequestReceived  -= OnRequestReceived;
        _chatService.FriendRequestResponded -= OnRequestResponded;
    }

    private void OnRequestReceived(object? sender, FriendRequestNotification dto)
        => RequestReceived?.Invoke(this, ToModel(dto));

    private void OnRequestResponded(object? sender, FriendRequestNotification dto)
        => RequestResponded?.Invoke(this, ToModel(dto));

    private static FriendRequest ToModel(FriendRequestDto dto) => new()
    {
        RequestId      = dto.Id.ToString(),
        SenderId       = dto.FromUserId.ToString(),
        SenderUsername = dto.FromUsername,
        SenderName     = dto.FromDisplayName,
        ReceiverId     = dto.ToUserId.ToString(),
        ReceiverName   = dto.ToDisplayName,
        Status         = Enum.TryParse<FriendRequestStatus>(dto.Status, out var s)
                             ? s : FriendRequestStatus.Pending,
        CreatedAt      = dto.CreatedAt
    };

    private static FriendRequest ToModel(FriendRequestNotification dto) => new()
    {
        RequestId      = dto.Id.ToString(),
        SenderId       = dto.FromUserId.ToString(),
        SenderUsername = dto.FromUsername,
        SenderName     = dto.FromDisplayName,
        ReceiverId     = dto.ToUserId.ToString(),
        ReceiverName   = dto.ToDisplayName,
        Status         = Enum.TryParse<FriendRequestStatus>(dto.Status, out var s)
                             ? s : FriendRequestStatus.Pending,
        CreatedAt      = dto.CreatedAt
    };

    private record FriendRequestDto(
        Guid Id,
        Guid FromUserId,
        string FromUsername,
        string FromDisplayName,
        Guid ToUserId,
        string ToUsername,
        string ToDisplayName,
        string Status,
        string? Message,
        DateTime CreatedAt,
        DateTime? RespondedAt);
}
