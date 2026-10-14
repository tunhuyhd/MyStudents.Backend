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

public record CreateClassScheduleInput(DayOfWeek DayOfWeek, TimeOnly StartTime, decimal DurationHours);
public record CreateClassCommand(string Name, string Code, CategoryOfClass Category, Guid SubjectId,
    DateOnly StartDate, DateOnly ExpectedEndDate, string? LinkOnline = null,
    List<CreateClassScheduleInput>? Schedules = null) : IRequest<Guid>, IClassInput;

public class CreateClassCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, ClassroomService classroom)
    : IRequestHandler<CreateClassCommand, Guid>
{
    public async Task<Guid> Handle(CreateClassCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (!await db.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken))
            throw new NotFoundException("Subject", request.SubjectId);
        var schedules = request.Schedules ?? [];
        var sessions = ClassroomService.BuildPlan(request.StartDate, request.ExpectedEndDate, schedules);
        await classroom.CheckConflictsAsync(sessions, null, null, cancellationToken);
        var entity = new Class
        {
            Name = request.Name.Trim(), Code = request.Code.Trim(), CategoryOfClass = request.Category,
            SubjectId = request.SubjectId, TeacherId = userId, StartDate = request.StartDate, ExpectedEndDate = request.ExpectedEndDate,
            LinkOnline = ClassroomService.NormalizeLink(request.Category, request.LinkOnline), Sessions = sessions,
            Schedules = schedules.Select(s => new ClassSchedule { DayOfWeek = s.DayOfWeek, StartTime = s.StartTime, DurationHours = s.DurationHours }).ToList()
        };
        db.Classes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return entity.Id;
    }
}

