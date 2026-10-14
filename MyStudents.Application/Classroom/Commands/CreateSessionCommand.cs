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

public record CreateSessionCommand : IRequest<Guid>, ISessionInput
{
    public Guid ClassId { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string? Note { get; set; }
}
public class CreateSessionCommandHandler(IApplicationDbContext db, ClassroomService classroom) : IRequestHandler<CreateSessionCommand, Guid>
{
    public async Task<Guid> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var entity = await db.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId, cancellationToken) ?? throw new NotFoundException("Class", request.ClassId);
        if (entity.Status != ClassStatus.Active) throw new BusinessException("ERR_CLASS_NOT_ACTIVE");
        if (request.Date < entity.StartDate || request.Date > entity.ExpectedEndDate) throw new BusinessException("ERR_SESSION_OUTSIDE_CLASS");
        var session = new ClassSession { ClassId = entity.Id, Date = request.Date, StartTime = request.StartTime, EndTime = request.EndTime, Note = request.Note, IsManual = true };
        await classroom.CheckConflictsAsync([session], null, null, cancellationToken);
        session.OrderIndex = (await db.ClassSessions.Where(s => s.ClassId == entity.Id).Select(s => (int?)s.OrderIndex).MaxAsync(cancellationToken) ?? 0) + 1;
        db.ClassSessions.Add(session);
        await classroom.AddAttendancesAsync(entity.Id, [session], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return session.Id;
    }
}

