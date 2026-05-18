using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SecureChat.Application.Common;

namespace SecureChat.Infrastructure.Identity;

public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    // sub → ClaimTypes.NameIdentifier (기본 JWT 클레임 매핑)
    public Guid UserId =>
        Guid.Parse(_accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User not authenticated"));

    public string Username =>
        _accessor.HttpContext?.User.FindFirstValue("preferred_username") ?? string.Empty;
}
