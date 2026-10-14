using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Classroom.Services;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Common.Validation;
using MyStudents.Domain.Entities;
using MyStudents.Domain.Entities.Enum;

namespace MyStudents.Application.Classroom.Commands;

public record UpdateSessionCommand : IRequest<bool>, ISessionInput
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string? Note { get; set; }
}
public class UpdateSessionCommandHandler(IApplicationDbContext db, ClassroomService classroom) : IRequestHandler<UpdateSessionCommand, bool>
{
    public async Task<bool> Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var session = await db.ClassSessions.Include(s => s.Class).Include(s => s.Attendances)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken) ?? throw new NotFoundException("Session", request.Id);
        var changed = session.Date != request.Date || session.StartTime != request.StartTime || session.EndTime != request.EndTime;
        if (changed)
        {
            if (classroom.HasHistory(session)) throw new BusinessException("ERR_SESSION_HISTORY_LOCKED");
            if (session.Class.Status != ClassStatus.Active) throw new BusinessException("ERR_CLASS_NOT_ACTIVE");
            if (request.Date < session.Class.StartDate || request.Date > session.Class.ExpectedEndDate) throw new BusinessException("ERR_SESSION_OUTSIDE_CLASS");
            await classroom.CheckConflictsAsync([new ClassSession { Date = request.Date, StartTime = request.StartTime, EndTime = request.EndTime }], null, session.Id, cancellationToken);
            session.Date = request.Date; session.StartTime = request.StartTime; session.EndTime = request.EndTime;
            // Rebuild only unrecorded attendance when moving a session across enrollment dates.
            db.Attendances.RemoveRange(session.Attendances);
            await db.SaveChangesAsync(cancellationToken);
            await classroom.AddAttendancesAsync(session.ClassId, [session], cancellationToken);
        }
        session.IsManual = true;
        session.Note = request.Note;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}

