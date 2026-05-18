namespace SecureChat.Models;

public record FriendRequestNotification(
    Guid Id,
    Guid FromUserId,
    string FromUsername,
    string FromDisplayName,
    Guid ToUserId,
    string ToUsername,
    string ToDisplayName,
    string Status,
    string? Message,
    DateTime CreatedAt,
    DateTime? RespondedAt);
