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
public class ClassesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ClassDto>>> GetList()
    {
        return await mediator.Send(new GetClassesQuery());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClassDto>> GetById(Guid id)
    {
        return await mediator.Send(new GetClassByIdQuery(id));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateClassCommand command)
    {
        return await mediator.Send(command);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateClassCommand command)
    {
        if (id != command.Id) return BadRequest();
        await mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteClassCommand(id));
        return NoContent();
    }
}
