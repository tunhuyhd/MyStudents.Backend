using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Classroom.Commands;
using MyStudents.Application.Classroom.Queries;
using MyStudents.Application.Classroom.Dto;
using MyStudents.Application.Common.Models;
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
    public async Task<ActionResult<PaginatedList<ClassDto>>> GetList(
        [FromQuery] string? searchTerm,
        [FromQuery] int? year,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDescending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        return await mediator.Send(new GetClassesQuery 
        { 
            SearchTerm = searchTerm,
            Year = year,
            SortBy = sortBy,
            SortDescending = sortDescending,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
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

    [HttpGet("{id}/students")]
    public async Task<ActionResult<PaginatedList<StudentSummaryDto>>> GetStudents(Guid id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        return await mediator.Send(new GetClassStudentsQuery { ClassId = id, PageNumber = pageNumber, PageSize = pageSize });
    }
}
