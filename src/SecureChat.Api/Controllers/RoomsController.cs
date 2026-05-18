using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Application.Common;
using SecureChat.Application.Messages;
using SecureChat.Application.Rooms;

namespace SecureChat.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly IMessageService _messageService;
    private readonly ICurrentUser _currentUser;

    public RoomsController(IRoomService roomService, IMessageService messageService, ICurrentUser currentUser)
    {
        _roomService = roomService;
        _messageService = messageService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyRooms(CancellationToken ct)
    {
        var result = await _roomService.GetMyRoomsAsync(_currentUser.UserId, ct);
        return Ok(result.Value);
    }

    [HttpGet("{roomId:guid}")]
    public async Task<IActionResult> GetRoom(Guid roomId, CancellationToken ct)
    {
        var result = await _roomService.GetRoomAsync(_currentUser.UserId, roomId, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 403);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroupRoom(CreateGroupRoomRequest request, CancellationToken ct)
    {
        var result = await _roomService.CreateGroupRoomAsync(_currentUser.UserId, request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetRoom), new { roomId = result.Value!.Id }, result.Value)
            : Problem(detail: result.Error, statusCode: 422);
    }

    [HttpPost("direct")]
    public async Task<IActionResult> GetOrCreateDirect(CreateDirectRoomRequest request, CancellationToken ct)
    {
        var (result, created) = await _roomService.GetOrCreateDirectRoomAsync(
            _currentUser.UserId, request.OtherUserId, ct);

        if (!result.IsSuccess)
            return Problem(detail: result.Error, statusCode: 422);

        return created
            ? CreatedAtAction(nameof(GetRoom), new { roomId = result.Value!.Id }, result.Value)
            : Ok(result.Value);
    }

    [HttpPost("{roomId:guid}/invite")]
    public async Task<IActionResult> InviteMember(Guid roomId, InviteMemberRequest request, CancellationToken ct)
    {
        var result = await _roomService.InviteAsync(_currentUser.UserId, roomId, request.UserId, ct);
        if (!result.IsSuccess)
        {
            var status = result.Error!.Contains("authorized") || result.Error.Contains("Not a room member") ? 403 : 422;
            return Problem(detail: result.Error, statusCode: status);
        }
        return Ok(result.Value);
    }

    [HttpPost("{roomId:guid}/transfer-admin")]
    public async Task<IActionResult> TransferAdmin(Guid roomId, TransferAdminRequest request, CancellationToken ct)
    {
        var result = await _roomService.TransferAdminAsync(_currentUser.UserId, roomId, request.NewAdminUserId, ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: result.Error!.Contains("방장만") ? 403 : 422);
    }

    [HttpDelete("{roomId:guid}/members/{targetUserId:guid}")]
    public async Task<IActionResult> KickMember(Guid roomId, Guid targetUserId, CancellationToken ct)
    {
        var result = await _roomService.KickMemberAsync(_currentUser.UserId, roomId, targetUserId, ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: result.Error!.Contains("권한") ? 403 : 422);
    }

    [HttpDelete("{roomId:guid}/leave")]
    public async Task<IActionResult> LeaveRoom(Guid roomId, CancellationToken ct)
    {
        var result = await _roomService.LeaveRoomAsync(_currentUser.UserId, roomId, ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(detail: result.Error, statusCode: 403);
    }

    /// <summary>
    /// 커서 페이지네이션으로 메시지 조회.
    /// ?before=cursor → 과거 이력 / ?after=cursor → 재연결 후 미수신. 동시 사용 불가.
    /// </summary>
    [HttpGet("{roomId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid roomId,
        [FromQuery] DateTime? before,
        [FromQuery] DateTime? after,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        if (before.HasValue && after.HasValue)
            return Problem("Cannot use 'before' and 'after' simultaneously", statusCode: 400);

        limit = Math.Clamp(limit, 1, 50);

        var result = await _messageService.GetPageAsync(
            _currentUser.UserId, roomId, before, after, limit, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 403);
    }
}
