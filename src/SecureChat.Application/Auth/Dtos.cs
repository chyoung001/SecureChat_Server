using SecureChat.Application.Users;

namespace SecureChat.Application.Auth;

public record RegisterRequest(string Username, string Password, string? Email, string? DisplayName);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);
