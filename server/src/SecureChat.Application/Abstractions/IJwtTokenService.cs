using SecureChat.Domain.Entities;

namespace SecureChat.Application.Abstractions;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
