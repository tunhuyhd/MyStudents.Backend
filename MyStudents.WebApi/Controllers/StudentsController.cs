using Microsoft.AspNetCore.Mvc;
using MyStudents.Application.Students.Commands;

namespace MyStudents.WebApi.Controllers;

public class StudentsController : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateStudentCommand command)
    {
        return await Mediator.Send(command);
    }
}
