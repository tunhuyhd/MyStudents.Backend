using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Students.Commands;
using MyStudents.Application.Students.Queries;
using MyStudents.Application.Students.Dto;
using MediatR;

namespace MyStudents.WebApi.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<List<StudentDto>>> GetList()
    {
        return await Mediator.Send(new GetStudentsQuery());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentDto>> Get(Guid id)
    {
        return await Mediator.Send(new GetStudentByIdQuery(id));
    }
}
