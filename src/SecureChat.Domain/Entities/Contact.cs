namespace SecureChat.Domain.Entities;

public class Contact
{
    public Guid OwnerUserId { get; private set; }
    public Guid ContactUserId { get; private set; }
    public bool IsBlocked { get; private set; }
    public string? Nickname { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User Owner { get; private set; } = null!;
    public User ContactUser { get; private set; } = null!;

    private Contact() { }

    public static Contact Create(Guid ownerUserId, Guid contactUserId)
    {
        return new Contact
        {
            OwnerUserId = ownerUserId,
            ContactUserId = contactUserId,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetBlocked(bool isBlocked) => IsBlocked = isBlocked;
    public void SetNickname(string? nickname) => Nickname = nickname;
}
