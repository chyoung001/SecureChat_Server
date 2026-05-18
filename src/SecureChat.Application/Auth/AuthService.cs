using FluentValidation;
using SecureChat.Application.Abstractions;
using SecureChat.Application.Common;
using SecureChat.Application.Users;
using SecureChat.Domain.Entities;

namespace SecureChat.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IRealtimeNotifier _notifier;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt,
        IRealtimeNotifier notifier,
        IValidator<RegisterRequest> registerValidator, IValidator<LoginRequest> loginValidator)
    {
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
        _notifier = notifier;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<AuthResponse>.Failure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var existing = await _uow.Users.FindByUsernameAsync(request.Username, ct);
        if (existing != null)
            return Result<AuthResponse>.Failure("이미 사용 중인 사용자명입니다.");

        var user = User.Create(request.Username, _hasher.Hash(request.Password), request.Email);
        if (request.DisplayName != null)
            user.UpdateProfile(request.DisplayName);

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        var (token, expiresAt) = _jwt.GenerateToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(token, expiresAt, ToDto(user)));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<AuthResponse>.Failure("사용자명 또는 비밀번호가 올바르지 않습니다.");

        var user = await _uow.Users.FindByUsernameAsync(request.Username, ct);
        if (user == null || !_hasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure("사용자명 또는 비밀번호가 올바르지 않습니다.");

        // 로그인 시 TokenVersion 증가 → 기존 세션 HTTP 토큰 무효화
        user.IncrementTokenVersion();
        user.UpdateLastSeen();
        await _uow.SaveChangesAsync(ct);

        // 기존 SignalR 세션이 살아있을 수 있으므로 강제 종료 알림 전송
        await _notifier.SendToUserAsync(user.Id, "SessionInvalidated", new { reason = "다른 기기에서 로그인되었습니다." }, ct);

        var (token, expiresAt) = _jwt.GenerateToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(token, expiresAt, ToDto(user)));
    }

    public async Task<Result<UserDto>> MeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _uow.Users.FindByIdAsync(userId, ct);
        return user is null
            ? Result<UserDto>.Failure("User not found")
            : Result<UserDto>.Success(ToDto(user));
    }

    public async Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _uow.Users.FindByIdAsync(userId, ct);
        if (user is null)
            return Result.Failure("User not found");

        user.IncrementTokenVersion();
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static UserDto ToDto(User u) =>
        new(u.Id, u.Username, u.DisplayName, u.Email, u.KeyFingerprint, u.LastSeenAt);
}
