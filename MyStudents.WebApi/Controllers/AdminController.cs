using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Admin.Commands;
using MyStudents.Application.Admin.Queries;
using MyStudents.Domain.Constants;
using System.Threading.Tasks;

namespace MyStudents.WebApi.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await mediator.Send(new GetUsersQuery());
        return Ok(result);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var result = await mediator.Send(new GetRolesQuery());
        return Ok(result);
    }

    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> UpdateRole(Guid userId, [FromBody] Guid newRoleId)
    {
        var result = await mediator.Send(new UpdateUserRoleCommand(userId, newRoleId));
        if (!result) return NotFound();
        return Ok();
    }

    [HttpPatch("users/{userId}/status")]
    public async Task<IActionResult> UpdateStatus(Guid userId, [FromBody] UpdateUserStatusCommand command)
    {
        if (userId != command.UserId) return BadRequest();
        var result = await mediator.Send(command);
        if (!result) return NotFound();
        return Ok();
    }
}
