using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Classroom.Commands;
using MyStudents.Application.Classroom.Queries;
using MyStudents.Application.Classroom.Dto;
using MediatR;

namespace MyStudents.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class SessionsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateSessionCommand command)
    {
        return await mediator.Send(command);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SessionDto?>> GetSession(Guid id)
    {
        return await mediator.Send(new GetSessionByIdQuery(id));
    }

    [HttpGet("{id}/attendances")]
    public async Task<ActionResult<List<AttendanceDto>>> GetAttendances(Guid id)
    {
        return await mediator.Send(new GetSessionAttendancesQuery(id));
    }

    [HttpPut("attendances")]
    public async Task<ActionResult<bool>> UpdateAttendance(UpdateAttendanceCommand command)
    {
        return await mediator.Send(command);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<bool>> Update(Guid id, UpdateSessionCommand command)
    {
        if (id != command.Id) return BadRequest();
        return await mediator.Send(command);
    }
}
