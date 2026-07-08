using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SecureChat.Models;
using SecureChat.Storage;

namespace SecureChat.Services;

public class HttpContactService : IContactService
{
    private readonly ApiHttpClient _apiClient;
    private readonly VerifiedKeyStore _verifiedKeys;
    private readonly ILogger<HttpContactService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HttpContactService(ApiHttpClient apiClient, VerifiedKeyStore verifiedKeys,
        ILogger<HttpContactService> logger)
    {
        _apiClient = apiClient;
        _verifiedKeys = verifiedKeys;
        _logger = logger;
    }

    public async Task<List<Contact>> GetContactsAsync()
    {
        try
        {
            var dtos = await _apiClient.Client.GetFromJsonAsync<List<ContactDto>>("/api/contacts", _json);
            return dtos?.Select(ToModel).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetContacts failed");
            return new();
        }
    }

    public async Task<Contact?> SearchUserAsync(string username)
    {
        try
        {
            var dtos = await _apiClient.Client.GetFromJsonAsync<List<UserDto>>(
                $"/api/users/search?q={Uri.EscapeDataString(username)}&limit=5", _json);
            var match = dtos?.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return match is null ? null : new Contact
            {
                UserId      = match.Id.ToString(),
                Username    = match.Username,
                DisplayName = match.DisplayName,
                KeyFingerprint = match.KeyFingerprint ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchUser failed");
            return null;
        }
    }

    public async Task AddContactAsync(string userId)
    {
        var response = await _apiClient.Client.PostAsJsonAsync("/api/contacts",
            new { userId = Guid.Parse(userId) });
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("AddContact returned {StatusCode}", response.StatusCode);
    }

    public async Task<Contact?> GetContactPublicKeyAsync(string userId)
    {
        try
        {
            var dto = await _apiClient.Client.GetFromJsonAsync<PublicKeyDto>(
                $"/api/users/{userId}/public-key", _json);
            return dto is null ? null : new Contact
            {
                UserId         = userId,
                PublicKeyPem   = dto.PublicKeyPem,
                KeyFingerprint = dto.KeyFingerprint
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetContactPublicKey failed for {UserId}", userId);
            return null;
        }
    }

    // D12: 키 지문 수동 검증. 서버 API가 없으므로 로컬에 fingerprint를 영속 저장한다.
    // 호출자는 검증 시점의 fingerprint를 함께 알아야 한다 — 인터페이스를 깨지 않기 위해
    // 가장 최근에 조회된 키를 사용한다. 이후 fingerprint가 바뀌면 재검증이 필요하다.
    public async Task MarkVerifiedAsync(string userId)
    {
        var key = await GetContactPublicKeyAsync(userId);
        if (key is null || string.IsNullOrEmpty(key.KeyFingerprint))
        {
            _logger.LogWarning("MarkVerified skipped: no public key for {UserId}", userId);
            return;
        }
        _verifiedKeys.MarkVerified(userId, key.KeyFingerprint);
    }

    public async Task RemoveContactAsync(string userId)
    {
        try
        {
            await _apiClient.Client.DeleteAsync($"/api/contacts/{userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveContact failed for {UserId}", userId);
        }
    }

    public async Task BlockContactAsync(string userId)
    {
        try
        {
            await _apiClient.Client.PatchAsJsonAsync($"/api/contacts/{userId}/block",
                new { isBlocked = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlockContact failed for {UserId}", userId);
        }
    }

    private static Contact ToModel(ContactDto dto) => new()
    {
        UserId      = dto.UserId.ToString(),
        Username    = dto.Username,
        DisplayName = dto.DisplayName,
        IsBlocked   = dto.IsBlocked,
        KeyFingerprint = string.Empty // 연락처 목록 API에는 키 없음; 필요 시 별도 조회
    };

    private record ContactDto(Guid UserId, string Username, string DisplayName,
        string? Nickname, bool IsBlocked);
    private record UserDto(Guid Id, string Username, string DisplayName,
        string? Email, string? KeyFingerprint, DateTime LastSeenAt);
    private record PublicKeyDto(Guid UserId, string PublicKeyPem, string KeyFingerprint);
}
