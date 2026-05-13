using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Subjects.Commands;
using MyStudents.Application.Subjects.Queries;
using MyStudents.Application.Subjects.Dto;
using MyStudents.Domain.Constants;
using MediatR;

namespace MyStudents.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SubjectsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize] // Both Admin and User can call
    public async Task<ActionResult<List<SubjectDto>>> GetList()
    {
        return await mediator.Send(new GetSubjectsQuery());
    }

    [HttpGet("{id}")]
    [Authorize] // Both Admin and User can call
    public async Task<ActionResult<SubjectDto>> GetById(Guid id)
    {
        return await mediator.Send(new GetSubjectByIdQuery(id));
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)] // Only Admin
    public async Task<ActionResult<Guid>> Create(CreateSubjectCommand command)
    {
        return await mediator.Send(command);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.Admin)] // Only Admin
    public async Task<ActionResult> Update(Guid id, UpdateSubjectCommand command)
    {
        if (id != command.Id) return BadRequest();
        await mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.Admin)] // Only Admin
    public async Task<ActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteSubjectCommand(id));
        return NoContent();
    }
}
