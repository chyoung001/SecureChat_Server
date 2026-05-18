namespace SecureChat.Storage;

public class AppSettings
{
    public string ServerUrl { get; set; } = "https://securechatserver-production.up.railway.app";
    public string HubPath { get; set; } = "/hubs/chat";
    public bool ScreenCaptureProtectionEnabled { get; set; } = true;
    public int DefaultTtlSeconds { get; set; }
}
