using MediatR;
using MyStudents.Domain.Common;
using MyStudents.Domain.Entities;

namespace MyStudents.Application.Students.Commands;

public record CreateStudentCommand : IRequest<Guid>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public string StudentIdNumber { get; init; } = string.Empty;
    public Guid ClassId { get; init; }
}

public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, Guid>
{
    private readonly IRepository<Student> _studentRepository;

    public CreateStudentCommandHandler(IRepository<Student> studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<Guid> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        var student = new Student
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            StudentIdNumber = request.StudentIdNumber,
            ClassId = request.ClassId
        };

        await _studentRepository.AddAsync(student, cancellationToken);
        await _studentRepository.SaveChangesAsync(cancellationToken);

        return student.Id;
    }
}
