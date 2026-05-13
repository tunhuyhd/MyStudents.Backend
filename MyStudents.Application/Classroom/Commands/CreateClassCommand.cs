using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyStudents.Application.Classroom.Commands;

public class CreateClassCommand : IRequest
{
	public string Name { get; set; }
	public string Description { get; set; }
}
