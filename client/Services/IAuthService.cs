using SecureChat.Models;

namespace SecureChat.Services;

public interface IAuthService
{
    event EventHandler<bool>? AuthStateChanged;
    UserProfile? CurrentUser { get; }
    bool IsAuthenticated { get; }

    Task<bool> LoginAsync(string username, string password, string serverUrl);
    Task<bool> SignUpAsync(string username, string password, string serverUrl);
    Task LogoutAsync();
    string? GetAccessToken();
}
