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
        var attendance = await context.Attendances
            .FirstOrDefaultAsync(a => a.Id == request.AttendanceId, cancellationToken);

        if (attendance == null) return false;

        attendance.Status = request.Status;
        if (request.Note != null)
        {
            attendance.Note = request.Note;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
