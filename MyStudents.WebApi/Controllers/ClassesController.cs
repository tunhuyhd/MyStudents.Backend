using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Classroom.Commands;
using MyStudents.Application.Classroom.Queries;
using MyStudents.Application.Classroom.Dto;
using MediatR;
using MyStudents.Domain.Entities.Enum;
using MyStudents.Application.Students.Dto;

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

    [HttpPost("{id}/students")]
    public async Task<ActionResult<bool>> AddStudent(Guid id, [FromBody] Guid studentId)
    {
        return await mediator.Send(new AddStudentToClassCommand { ClassId = id, StudentId = studentId });
    }

    [HttpPut("{id}/students/{studentId}")]
    public async Task<ActionResult<bool>> UpdateStudentStatus(Guid id, Guid studentId, [FromBody] StudentClassStatus status)
    {
        return await mediator.Send(new UpdateStudentInClassCommand { ClassId = id, StudentId = studentId, Status = status });
    }

    [HttpDelete("{id}/students/{studentId}")]
    public async Task<ActionResult<bool>> RemoveStudent(Guid id, Guid studentId)
    {
        return await mediator.Send(new RemoveStudentFromClassCommand { ClassId = id, StudentId = studentId });
    }

    [HttpGet("{id}/available-students")]
    public async Task<ActionResult<List<StudentDto>>> GetAvailableStudents(Guid id, string? searchTerm)
    {
        return await mediator.Send(new GetAvailableStudentsForClassQuery { ClassId = id, SearchTerm = searchTerm });
    }
}
