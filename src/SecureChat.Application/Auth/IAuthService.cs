using SecureChat.Application.Common;
using SecureChat.Application.Users;

namespace SecureChat.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<UserDto>> MeAsync(Guid userId, CancellationToken ct = default);
    Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default);
}
