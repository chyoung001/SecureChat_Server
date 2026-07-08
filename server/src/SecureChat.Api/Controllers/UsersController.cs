using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Application.Common;
using SecureChat.Application.Users;

namespace SecureChat.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUser _currentUser;

    public UsersController(IUserService userService, ICurrentUser currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await _userService.SearchAsync(q, limit, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(userId, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await _userService.UpdateProfileAsync(_currentUser.UserId, request, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("{userId:guid}/public-key")]
    public async Task<IActionResult> GetPublicKey(Guid userId, CancellationToken ct)
    {
        var result = await _userService.GetPublicKeyAsync(userId, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("me/public-key")]
    public async Task<IActionResult> UpdatePublicKey([FromBody] UpdatePublicKeyRequest request, CancellationToken ct)
    {
        var result = await _userService.UpdatePublicKeyAsync(_currentUser.UserId, request, ct);
        if (result.IsSuccess) return NoContent();
        // 키 회전 시도는 409 Conflict로 명시. 그 외 오류는 400.
        if (result.Error!.Contains("already registered", StringComparison.OrdinalIgnoreCase))
            return Problem(detail: result.Error, statusCode: 409);
        return BadRequest(new { error = result.Error });
    }
}
