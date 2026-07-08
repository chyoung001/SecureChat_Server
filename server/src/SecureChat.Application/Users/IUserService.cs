using SecureChat.Application.Common;

namespace SecureChat.Application.Users;

public interface IUserService
{
    Task<Result<List<UserDto>>> SearchAsync(string query, int limit, CancellationToken ct = default);
    Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateProfileAsync(Guid callerId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<Result<PublicKeyDto>> GetPublicKeyAsync(Guid userId, CancellationToken ct = default);
    Task<Result> UpdatePublicKeyAsync(Guid callerId, UpdatePublicKeyRequest request, CancellationToken ct = default);
}
