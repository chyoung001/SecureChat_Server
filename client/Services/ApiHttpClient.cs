using System.Net.Http.Headers;
using SecureChat.Storage;

namespace SecureChat.Services;

// 모든 HTTP 요청에 현재 Bearer 토큰을 자동 부착하는 DelegatingHandler.
internal sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly TokenStorage _tokenStorage;

    public BearerTokenHandler(TokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _tokenStorage.LoadToken();
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return base.SendAsync(request, cancellationToken);
    }
}

// 싱글톤 HttpClient. AppSettings.ServerUrl을 BaseAddress로 사용한다.
// 로그인 시점에 BaseAddress가 확정되므로 첫 호출 시 lazy로 초기화한다.
// DEBUG 빌드에서만 자체 서명 인증서를 허용한다.
public sealed class ApiHttpClient : IDisposable
{
    private readonly AppSettings _appSettings;
    private readonly TokenStorage _tokenStorage;
    private HttpClient? _client;
    private readonly object _lock = new();

    public ApiHttpClient(AppSettings appSettings, TokenStorage tokenStorage)
    {
        _appSettings = appSettings;
        _tokenStorage = tokenStorage;
    }

    public HttpClient Client
    {
        get
        {
            if (_client is not null) return _client;
            lock (_lock)
            {
                _client ??= Build(_appSettings.ServerUrl, _tokenStorage);
                return _client;
            }
        }
    }

    // 로그인/회원가입처럼 토큰 없이 BaseAddress가 다른 호출용.
    // 핸들러는 매번 새로 만들어도 비용이 작다 (소켓풀은 OS가 관리).
    public static HttpClient CreateUnauthenticated(string serverUrl)
    {
        var handler = new HttpClientHandler();
#if DEBUG
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(serverUrl.TrimEnd('/'))
        };
    }

    private static HttpClient Build(string serverUrl, TokenStorage tokenStorage)
    {
        var primaryHandler = new HttpClientHandler();
#if DEBUG
        primaryHandler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif

        var bearerHandler = new BearerTokenHandler(tokenStorage)
        {
            InnerHandler = primaryHandler
        };

        return new HttpClient(bearerHandler)
        {
            BaseAddress = new Uri(serverUrl.TrimEnd('/'))
        };
    }

    public void Dispose() => _client?.Dispose();
}
