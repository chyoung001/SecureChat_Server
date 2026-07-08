namespace SecureChat.Application.Contacts;

public record ContactDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? Nickname,
    bool IsBlocked);

public record AddContactRequest(Guid UserId);
public record BlockContactRequest(bool IsBlocked);
