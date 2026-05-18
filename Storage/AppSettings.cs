namespace SecureChat.Storage;

public class AppSettings
{
    public string ServerUrl { get; set; } = "https://chat.example.com";
    public string HubPath { get; set; } = "/hubs/chat";
    public bool ScreenCaptureProtectionEnabled { get; set; } = true;
    public int DefaultTtlSeconds { get; set; }
}
