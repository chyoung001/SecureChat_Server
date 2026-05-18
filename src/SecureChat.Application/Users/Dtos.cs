namespace SecureChat.Application.Users;

public record UserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string? Email,
    string? KeyFingerprint,
    DateTime LastSeenAt);

public record UpdateProfileRequest(string? DisplayName);
public record PublicKeyDto(Guid UserId, string PublicKeyPem, string KeyFingerprint);
public record UpdatePublicKeyRequest(string PublicKeyPem);
