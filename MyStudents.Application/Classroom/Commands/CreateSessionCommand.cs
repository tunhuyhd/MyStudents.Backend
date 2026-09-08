using MyStudents.Application.Common.Exceptions;
using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;
using MyStudents.Domain.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Commands;

public record CreateSessionCommand : IRequest<Guid>
{
    public Guid ClassId { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string? Note { get; set; }
}

public class CreateSessionCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateSessionCommand, Guid>
{
    public async Task<Guid> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        if (!await context.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
            throw new NotFoundException("Class", request.ClassId);

        var session = new ClassSession
        {
            ClassId = request.ClassId,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Note = request.Note,
            Status = SessionStatus.Scheduled
        };

        context.ClassSessions.Add(session);
        
        // When creating a session, we should also create empty attendance records for all students currently in the class
        var studentsInClass = await context.ClassStudents
            .Where(cs => cs.ClassId == request.ClassId && cs.Status == StudentClassStatus.Active)
            .Select(cs => cs.StudentId)
            .ToListAsync(cancellationToken);

        foreach (var studentId in studentsInClass)
        {
            context.Attendances.Add(new Attendance
            {
                Session = session,
                StudentId = studentId,
                Status = AttendanceStatus.Present // Default to Present or leave as is? Let's default to Present for convenience
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return session.Id;
    }
}
