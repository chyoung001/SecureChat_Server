using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Application.Common;
using SecureChat.Application.FriendRequests;

namespace SecureChat.Api.Controllers;

[ApiController]
[Route("api/friend-requests")]
[Authorize]
public class FriendRequestsController : ControllerBase
{
    private readonly IFriendRequestService _service;
    private readonly ICurrentUser _currentUser;

    public FriendRequestsController(IFriendRequestService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Send(SendFriendRequestRequest request, CancellationToken ct)
    {
        var result = await _service.SendAsync(_currentUser.UserId, request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetIncoming), null, result.Value)
            : Problem(detail: result.Error, statusCode: 422);
    }

    [HttpGet("incoming")]
    public async Task<IActionResult> GetIncoming(CancellationToken ct)
    {
        var result = await _service.GetIncomingAsync(_currentUser.UserId, ct);
        return Ok(result.Value);
    }

    [HttpGet("outgoing")]
    public async Task<IActionResult> GetOutgoing(CancellationToken ct)
    {
        var result = await _service.GetOutgoingAsync(_currentUser.UserId, ct);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        var result = await _service.AcceptAsync(_currentUser.UserId, id, ct);
        if (!result.IsSuccess)
        {
            var status = result.Error!.Contains("authorized") ? 403 : 422;
            return Problem(detail: result.Error, statusCode: status);
        }
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        var result = await _service.RejectAsync(_currentUser.UserId, id, ct);
        if (!result.IsSuccess)
        {
            var status = result.Error!.Contains("authorized") ? 403 : 422;
            return Problem(detail: result.Error, statusCode: status);
        }
        return Ok(result.Value);
    }
}
