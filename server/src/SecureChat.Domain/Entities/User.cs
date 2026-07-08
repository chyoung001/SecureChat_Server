using SecureChat.Domain.Common;

namespace SecureChat.Domain.Entities;

public class User : EntityBase
{
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? Email { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? PublicKeyPem { get; private set; }
    public string? KeyFingerprint { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }
    public int TokenVersion { get; private set; }

    private User() { }

    public static User Create(string username, string passwordHash, string? email = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = passwordHash,
            Email = email,
            DisplayName = username,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            TokenVersion = 0
        };
    }

    public void UpdateLastSeen() => LastSeenAt = DateTime.UtcNow;
    public void IncrementTokenVersion() => TokenVersion++;
    public void UpdateProfile(string? displayName)
    {
        if (displayName != null) DisplayName = displayName;
    }
    public void SetPublicKey(string publicKeyPem, string fingerprint)
    {
        PublicKeyPem = publicKeyPem;
        KeyFingerprint = fingerprint;
    }
}
