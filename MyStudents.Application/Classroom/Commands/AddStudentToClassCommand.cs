using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;
using MyStudents.Domain.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Commands;

public record AddStudentToClassCommand : IRequest<bool>
{
    public Guid ClassId { get; init; }
    public Guid StudentId { get; init; }
}

public class AddStudentToClassCommandHandler(IApplicationDbContext context) : IRequestHandler<AddStudentToClassCommand, bool>
{
    public async Task<bool> Handle(AddStudentToClassCommand request, CancellationToken cancellationToken)
    {
        // Check if already exists
        var exists = await context.ClassStudents
            .AnyAsync(x => x.ClassId == request.ClassId && x.StudentId == request.StudentId, cancellationToken);

        if (exists) return false;

        var classStudent = new ClassStudent
        {
            ClassId = request.ClassId,
            StudentId = request.StudentId,
            JoinedAt = DateTime.UtcNow,
            Status = StudentClassStatus.Active
        };

        context.ClassStudents.Add(classStudent);

        // Create attendances for all existing sessions of this class
        var sessions = await context.ClassSessions
            .Where(s => s.ClassId == request.ClassId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var sessionId in sessions)
        {
            context.Attendances.Add(new Attendance
            {
                SessionId = sessionId,
                StudentId = request.StudentId,
                Status = AttendanceStatus.Present
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
