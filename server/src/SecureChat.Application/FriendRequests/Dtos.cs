namespace SecureChat.Application.FriendRequests;

public record FriendRequestDto(
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

public record SendFriendRequestRequest(Guid ToUserId, string? Message);
