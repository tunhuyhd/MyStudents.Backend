using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities.Enum;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Commands;

public record UpdateAttendanceCommand : IRequest<bool>
{
    public Guid AttendanceId { get; init; }
    public AttendanceStatus Status { get; init; }
    public string? Note { get; init; }
}

public class UpdateAttendanceCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateAttendanceCommand, bool>
{
    public async Task<bool> Handle(UpdateAttendanceCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var attendance = await context.Attendances
            .Include(a => a.Session).ThenInclude(s => s.Class)
            .FirstOrDefaultAsync(a => a.Id == request.AttendanceId, cancellationToken);

        if (attendance == null) throw new MyStudents.Application.Common.Exceptions.NotFoundException("Attendance", request.AttendanceId);
        if (attendance.Session.Status == SessionStatus.Cancelled || attendance.Session.Class.Status == ClassStatus.Cancelled)
            throw new MyStudents.Application.Common.Exceptions.BusinessException("ERR_SESSION_CANCELLED");
        if (attendance.Session.Status == SessionStatus.Completed && request.Status == AttendanceStatus.Pending)
            throw new MyStudents.Application.Common.Exceptions.BusinessException("ERR_SESSION_HISTORY_LOCKED");

        attendance.Status = request.Status;
        if (request.Note != null)
        {
            attendance.Note = request.Note;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
