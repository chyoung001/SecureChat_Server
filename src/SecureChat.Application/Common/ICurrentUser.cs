namespace SecureChat.Application.Common;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Username { get; }
}
