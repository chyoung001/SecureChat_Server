using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SecureChat.Application.Common;

namespace SecureChat.Application.Users;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;

    public UserService(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<List<UserDto>>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        if (query.Length < 3)
            return Result<List<UserDto>>.Failure("Query must be at least 3 characters");

        if (query.Length > 32 || !Regex.IsMatch(query, @"^[a-zA-Z0-9_]+$"))
            return Result<List<UserDto>>.Failure("Query must match ^[a-zA-Z0-9_]+$ and be at most 32 characters");

        limit = Math.Clamp(limit, 1, 20);
        var users = await _uow.Users.SearchByUsernameAsync(query, limit, ct);
        return Result<List<UserDto>>.Success(users.Select(ToDto).ToList());
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _uow.Users.FindByIdAsync(userId, ct);
        return user is null
            ? Result<UserDto>.Failure("User not found")
            : Result<UserDto>.Success(ToDto(user));
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(Guid callerId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Users.FindByIdAsync(callerId, ct);
        if (user is null) return Result<UserDto>.Failure("User not found");

        user.UpdateProfile(request.DisplayName);
        await _uow.SaveChangesAsync(ct);
        return Result<UserDto>.Success(ToDto(user));
    }

    public async Task<Result<PublicKeyDto>> GetPublicKeyAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _uow.Users.FindByIdAsync(userId, ct);
        if (user is null) return Result<PublicKeyDto>.Failure("User not found");
        if (user.PublicKeyPem is null || user.KeyFingerprint is null)
            return Result<PublicKeyDto>.Failure("Public key not registered");
        return Result<PublicKeyDto>.Success(new PublicKeyDto(user.Id, user.PublicKeyPem, user.KeyFingerprint));
    }

    public async Task<Result> UpdatePublicKeyAsync(Guid callerId, UpdatePublicKeyRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Users.FindByIdAsync(callerId, ct);
        if (user is null) return Result.Failure("User not found");

        // D1: 키 회전은 신뢰 모델을 깨므로 최초 1회 등록만 허용.
        // 키를 잃어버린 경우 서버 관리자 개입(직접 DB에서 null로 리셋)이 필요.
        if (user.PublicKeyPem is not null)
        {
            // 같은 키 재등록은 멱등으로 허용 (재로그인 등에서 발생).
            if (string.Equals(user.PublicKeyPem, request.PublicKeyPem, StringComparison.Ordinal))
                return Result.Success();
            return Result.Failure("Public key already registered; key rotation is not allowed");
        }

        var fingerprint = ComputeFingerprint(request.PublicKeyPem);
        user.SetPublicKey(request.PublicKeyPem, fingerprint);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // D8: PEM 텍스트 바이트 대신 DER(SubjectPublicKeyInfo)에서 fingerprint 계산.
    // 줄바꿈/패딩 차이에 영향받지 않고 표준 도구(openssl 등)와 호환된다.
    private static string ComputeFingerprint(string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var der = rsa.ExportSubjectPublicKeyInfo();
        return Convert.ToHexString(SHA256.HashData(der)).ToLowerInvariant();
    }

    private static UserDto ToDto(Domain.Entities.User u) =>
        new(u.Id, u.Username, u.DisplayName, u.Email, u.KeyFingerprint, u.LastSeenAt);
}
