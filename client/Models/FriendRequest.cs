namespace SecureChat.Models;

public enum FriendRequestStatus { Pending, Accepted, Rejected }

public class FriendRequest
{
    public string              RequestId       { get; set; } = Guid.NewGuid().ToString();
    public string              SenderId        { get; set; } = string.Empty;
    public string              SenderUsername  { get; set; } = string.Empty;
    public string              SenderName      { get; set; } = string.Empty;
    public string              ReceiverId      { get; set; } = string.Empty;
    public string              ReceiverName    { get; set; } = string.Empty;
    public FriendRequestStatus Status          { get; set; } = FriendRequestStatus.Pending;
    public DateTime            CreatedAt       { get; set; } = DateTime.UtcNow;
}
