using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Students.Commands;

public record CreateStudentCommand : IRequest<Guid>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public string? School { get; init; }
    public string? ParentName { get; init; }
    public string? ParentPhone { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Note { get; init; }
}

public class CreateStudentCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateStudentCommand, Guid>
{
    public async Task<Guid> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        var student = new Student
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            School = request.School,
            ParentName = request.ParentName,
            ParentPhone = request.ParentPhone,
            Phone = request.Phone,
            Address = request.Address,
            Note = request.Note
        };

        context.Students.Add(student);
        await context.SaveChangesAsync(cancellationToken);

        return student.Id;
    }
}
