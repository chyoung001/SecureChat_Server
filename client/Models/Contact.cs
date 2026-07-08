namespace SecureChat.Models;

public class Contact
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
    public string KeyFingerprint { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsBlocked  { get; set; }
}
