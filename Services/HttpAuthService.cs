using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SecureChat.Crypto;
using SecureChat.Models;
using SecureChat.Storage;

namespace SecureChat.Services;

public class HttpAuthService : IAuthService
{
    public event EventHandler<bool>? AuthStateChanged;
    public UserProfile? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    private readonly TokenStorage _tokenStorage;
    private readonly AppSettings _appSettings;
    private readonly LocalKeyStore _keyStore;
    private readonly ApiHttpClient _apiClient;
    private readonly ILogger<HttpAuthService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HttpAuthService(TokenStorage tokenStorage, AppSettings appSettings,
        LocalKeyStore keyStore, ApiHttpClient apiClient, ILogger<HttpAuthService> logger)
    {
        _tokenStorage = tokenStorage;
        _appSettings = appSettings;
        _keyStore = keyStore;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string username, string password, string serverUrl)
    {
        using var client = ApiHttpClient.CreateUnauthenticated(serverUrl);
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            throw new InvalidOperationException("서버에 연결할 수 없습니다.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var msg = await ExtractErrorMessageAsync(response);
            _logger.LogWarning("Login failed: {StatusCode} — {Message}", response.StatusCode, msg);
            throw new InvalidOperationException(msg);
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_json);
        if (auth is null) throw new InvalidOperationException("서버 응답을 파싱할 수 없습니다.");

        ApplyAuthResponse(auth, serverUrl);
        await EnsurePublicKeyRegisteredAsync();
        return true;
    }

    public async Task<bool> SignUpAsync(string username, string password, string serverUrl)
    {
        using var client = ApiHttpClient.CreateUnauthenticated(serverUrl);
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/api/auth/register",
                new { username, password, email = (string?)null, displayName = (string?)null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignUp error");
            throw new InvalidOperationException("서버에 연결할 수 없습니다.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var msg = await ExtractErrorMessageAsync(response);
            _logger.LogWarning("SignUp failed: {StatusCode} — {Message}", response.StatusCode, msg);
            throw new InvalidOperationException(msg);
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_json);
        if (auth is null) throw new InvalidOperationException("서버 응답을 파싱할 수 없습니다.");

        ApplyAuthResponse(auth, serverUrl);
        await EnsurePublicKeyRegisteredAsync();
        return true;
    }

    public async Task LogoutAsync()
    {
        var token = _tokenStorage.LoadToken();
        if (token is not null && CurrentUser is not null)
        {
            try
            {
                await _apiClient.Client.PostAsync("/api/auth/logout", null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Logout HTTP call failed (ignored)");
            }
        }

        CurrentUser = null;
        _tokenStorage.ClearToken();
        AuthStateChanged?.Invoke(this, false);
    }

    public string? GetAccessToken() => _tokenStorage.LoadToken();

    // 로컬 키쌍이 없으면 생성 후 서버에 공개키 등록.
    // D1 정책: 서버는 최초 1회만 등록 허용 → 409 Conflict는 정상 (이미 등록된 상태)로 간주.
    private async Task EnsurePublicKeyRegisteredAsync()
    {
        var existing = _keyStore.LoadKeyPair();
        string publicPem;
        if (existing is null)
        {
            var (privatePem, pub) = E2ECrypto.GenerateKeyPair();
            _keyStore.SaveKeyPair(privatePem, pub);
            publicPem = pub;
            _logger.LogInformation("Generated new RSA-2048 key pair");
        }
        else
        {
            publicPem = existing.Value.publicPem;
        }

        try
        {
            var putResponse = await _apiClient.Client.PutAsJsonAsync("/api/users/me/public-key",
                new { publicKeyPem = publicPem });

            if (putResponse.StatusCode == HttpStatusCode.Conflict)
            {
                // 서버에 이미 다른 키가 등록되어 있음. 로컬 키와 서버 키가 다르면
                // 이 디바이스로는 본인 메시지조차 복호화 불가 — 사용자에게 알리는 게 맞지만
                // 현재 흐름에선 경고 로그만 남기고 진행 (UI 알림은 D12 작업과 함께).
                _logger.LogWarning(
                    "Server already has a public key for this user; local key may not match. " +
                    "Messages encrypted with the server-registered key cannot be decrypted on this device.");
            }
            else if (!putResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Public key registration returned {StatusCode}", putResponse.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Public key registration failed (non-fatal)");
        }
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(_json);
            if (!string.IsNullOrWhiteSpace(problem?.Detail)) return problem.Detail;
        }
        catch { /* JSON 파싱 실패 시 상태 코드 메시지로 폴백 */ }

        return response.StatusCode switch
        {
            HttpStatusCode.Conflict            => "이미 사용 중인 사용자명입니다.",
            HttpStatusCode.Unauthorized        => "사용자명 또는 비밀번호가 올바르지 않습니다.",
            HttpStatusCode.UnprocessableEntity => "입력값이 올바르지 않습니다.",
            HttpStatusCode.TooManyRequests     => "요청이 너무 많습니다. 잠시 후 다시 시도하세요.",
            HttpStatusCode.InternalServerError => "서버 오류가 발생했습니다.",
            _                                  => $"오류 ({(int)response.StatusCode})"
        };
    }

    private void ApplyAuthResponse(AuthResponse auth, string serverUrl)
    {
        _appSettings.ServerUrl = serverUrl.TrimEnd('/');
        _tokenStorage.SaveToken(auth.AccessToken);

        CurrentUser = new UserProfile
        {
            UserId = auth.User.Id.ToString(),
            Username = auth.User.Username,
            DisplayName = auth.User.DisplayName,
            LastSeenAt = auth.User.LastSeenAt,
            IsOnline = true
        };

        AuthStateChanged?.Invoke(this, true);
    }

    private record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);
    private record UserDto(Guid Id, string Username, string DisplayName,
        string? Email, string? KeyFingerprint, DateTime LastSeenAt);
    private record ProblemDetails(string? Detail);
}
