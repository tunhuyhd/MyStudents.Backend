using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Students.Commands;
using MyStudents.Application.Students.Queries;
using MyStudents.Application.Students.Dto;
using MediatR;
using MyStudents.Application.Common.Models;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.WebApi.Controllers;

[Authorize]
public class StudentsController : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateStudentCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Unit>> Update(Guid id, UpdateStudentCommand command)
    {
        if (id != command.Id) return BadRequest();
        return await Mediator.Send(command);
    }

    // Keep the original list response for existing clients during migration to the paged endpoint.
    [HttpGet]
    public async Task<ActionResult<List<StudentDto>>> GetList()
    {
        var page = await Mediator.Send(new GetStudentsQuery(PageSize: 100));
        var students = new List<StudentDto>(page.Items);
        for (var pageNumber = 2; pageNumber <= page.TotalPages; pageNumber++)
        {
            page = await Mediator.Send(new GetStudentsQuery(PageNumber: pageNumber, PageSize: 100));
            students.AddRange(page.Items);
        }
        return students;
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PaginatedList<StudentDto>>> GetPagedList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Status? status = null)
    {
        return await Mediator.Send(new GetStudentsQuery(pageNumber, pageSize, searchTerm, status));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentDto>> Get(Guid id)
    {
        return await Mediator.Send(new GetStudentByIdQuery(id));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<Unit>> UpdateStatus(Guid id, UpdateStudentStatusCommand command)
    {
        if (id != command.Id) return BadRequest();
        return await Mediator.Send(command);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Unit>> Delete(Guid id)
    {
        return await Mediator.Send(new DeleteStudentCommand { Id = id });
    }
}
