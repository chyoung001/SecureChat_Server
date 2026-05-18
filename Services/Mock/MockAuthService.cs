using SecureChat.Models;

namespace SecureChat.Services.Mock;

public class MockAuthService : IAuthService
{
    public event EventHandler<bool>? AuthStateChanged;

    public UserProfile? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    private string? _token;

    public async Task<bool> LoginAsync(string username, string password, string serverUrl)
    {
        await Task.Delay(500);

        CurrentUser = new UserProfile
        {
            UserId = Guid.NewGuid().ToString(),
            Username = username,
            DisplayName = username,
            LastSeenAt = DateTime.UtcNow,
            IsOnline = true
        };
        _token = $"mock-token-{Guid.NewGuid():N}";
        AuthStateChanged?.Invoke(this, true);
        return true;
    }

    public Task<bool> SignUpAsync(string username, string password, string serverUrl)
        => LoginAsync(username, password, serverUrl);

    public Task LogoutAsync()
    {
        CurrentUser = null;
        _token = null;
        AuthStateChanged?.Invoke(this, false);
        return Task.CompletedTask;
    }

    public string? GetAccessToken() => _token;
}
