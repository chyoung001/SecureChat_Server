namespace SecureChat.Models;

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LastSeenAt { get; set; }
    public bool IsOnline { get; set; }
}
