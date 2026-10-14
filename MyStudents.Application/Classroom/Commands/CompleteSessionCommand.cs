using System.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Classroom.Services;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities.Enum;
namespace MyStudents.Application.Classroom.Commands;

public record CompleteSessionCommand(Guid Id) : IRequest;
public class CompleteSessionCommandHandler(IApplicationDbContext db, ClassroomService classroom) : IRequestHandler<CompleteSessionCommand>
{
    public async Task Handle(CompleteSessionCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var session = await db.ClassSessions.Include(s => s.Attendances).Include(s => s.Class).FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Session", request.Id);
        if (session.Status == SessionStatus.Cancelled || session.Class.Status == ClassStatus.Cancelled)
            throw new BusinessException("ERR_SESSION_CANCELLED");
        if (session.Date > classroom.Today) throw new BusinessException("ERR_SESSION_NOT_STARTED");
        if (session.Attendances.Any(a => a.Status == AttendanceStatus.Pending)) throw new BusinessException("ERR_ATTENDANCE_PENDING");
        session.Status = SessionStatus.Completed;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

