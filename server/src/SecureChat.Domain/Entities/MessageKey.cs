namespace SecureChat.Domain.Entities;

public class MessageKey
{
    public Guid MessageId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string EncryptedAesKey { get; private set; } = null!;

    public Message Message { get; private set; } = null!;

    private MessageKey() { }

    public static MessageKey Create(Guid messageId, Guid recipientUserId, string encryptedAesKey)
    {
        return new MessageKey
        {
            MessageId = messageId,
            RecipientUserId = recipientUserId,
            EncryptedAesKey = encryptedAesKey
        };
    }
}
