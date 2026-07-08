using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Application.Common;
using SecureChat.Application.Contacts;

namespace SecureChat.Api.Controllers;

[ApiController]
[Route("api/contacts")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly ICurrentUser _currentUser;

    public ContactsController(IContactService contactService, ICurrentUser currentUser)
    {
        _contactService = contactService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyContacts(CancellationToken ct)
    {
        var result = await _contactService.GetMyContactsAsync(_currentUser.UserId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> AddContact([FromBody] AddContactRequest request, CancellationToken ct)
    {
        var result = await _contactService.AddContactAsync(_currentUser.UserId, request.UserId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetMyContacts), result.Value);
    }

    [HttpDelete("{contactUserId:guid}")]
    public async Task<IActionResult> RemoveContact(Guid contactUserId, CancellationToken ct)
    {
        var result = await _contactService.RemoveContactAsync(_currentUser.UserId, contactUserId, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return NoContent();
    }

    [HttpPatch("{contactUserId:guid}/block")]
    public async Task<IActionResult> ToggleBlock(Guid contactUserId, [FromBody] BlockContactRequest request, CancellationToken ct)
    {
        var result = await _contactService.ToggleBlockAsync(_currentUser.UserId, contactUserId, request.IsBlocked, ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }
}
