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

public record UpdateClassCommand(Guid Id, string Name, string Code, CategoryOfClass Category, Guid SubjectId,
    DateOnly StartDate, DateOnly ExpectedEndDate, ClassStatus Status, string? LinkOnline = null,
    List<CreateClassScheduleInput>? Schedules = null) : IRequest<Unit>, IClassInput;

public class UpdateClassCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, ClassroomService classroom)
    : IRequestHandler<UpdateClassCommand, Unit>
{
    public async Task<Unit> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var entity = await db.Classes.Include(c => c.Schedules).Include(c => c.Sessions).ThenInclude(s => s.Attendances)
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TeacherId == userId, cancellationToken)
            ?? throw new NotFoundException("Class", request.Id);
        if (!await db.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken))
            throw new NotFoundException("Subject", request.SubjectId);
        var schedules = request.Schedules ?? [];
        var oldSchedules = entity.Schedules.Select(s => new CreateClassScheduleInput(s.DayOfWeek, s.StartTime, s.DurationHours)).OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime);
        var newSchedules = schedules.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime);
        var changed = entity.StartDate != request.StartDate || entity.ExpectedEndDate != request.ExpectedEndDate || !oldSchedules.SequenceEqual(newSchedules);
        var added = new List<ClassSession>();
        var removed = new List<ClassSession>();
        if (changed)
        {
            // Validate weekly overlap even when the requested range is in the past.
            var desired = ClassroomService.BuildPlan(request.StartDate, request.ExpectedEndDate, schedules).Where(s => s.Date >= classroom.Today).ToList();
            var retained = entity.Sessions.Where(s => classroom.HasHistory(s) || s.IsManual).ToList();
            foreach (var session in entity.Sessions.Except(retained))
            {
                var match = desired.FirstOrDefault(s => SameSlot(s, session));
                if (match != null) { retained.Add(session); desired.Remove(match); }
                else removed.Add(session);
            }
            // Keep IDs and recorded attendance for exact matches; never fabricate replacement history.
            added = desired.Where(s => !retained.Any(r => SameSlot(s, r))).ToList();
            foreach (var session in added) session.ClassId = entity.Id;
            if (request.Status != ClassStatus.Cancelled)
                await classroom.CheckConflictsAsync(retained.Where(s => s.Date >= classroom.Today).Concat(added).ToList(), entity.Id, null, cancellationToken);
            foreach (var session in removed) db.Attendances.RemoveRange(session.Attendances);
            db.ClassSessions.RemoveRange(removed);
            db.ClassSchedules.RemoveRange(entity.Schedules);
            // Flush soft-deletes before inserting sessions with the same start time.
            await db.SaveChangesAsync(cancellationToken);
            foreach (var schedule in schedules)
                db.ClassSchedules.Add(new ClassSchedule { ClassId = entity.Id, DayOfWeek = schedule.DayOfWeek, StartTime = schedule.StartTime, DurationHours = schedule.DurationHours });
            db.ClassSessions.AddRange(added);
            await classroom.AddAttendancesAsync(entity.Id, added, cancellationToken);
        }
        else if (request.Status != ClassStatus.Cancelled && entity.Status == ClassStatus.Cancelled)
            await classroom.CheckConflictsAsync(entity.Sessions.Where(s => s.Date >= classroom.Today).ToList(), entity.Id, null, cancellationToken);

        entity.Name = request.Name.Trim(); entity.Code = request.Code.Trim(); entity.CategoryOfClass = request.Category;
        entity.SubjectId = request.SubjectId; entity.StartDate = request.StartDate; entity.ExpectedEndDate = request.ExpectedEndDate;
        entity.Status = request.Status; entity.LinkOnline = ClassroomService.NormalizeLink(request.Category, request.LinkOnline);
        var finalSessions = entity.Sessions.Except(removed).Concat(added).DistinctBy(s => s.Id).OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToList();
        for (var i = 0; i < finalSessions.Count; i++) finalSessions[i].OrderIndex = i + 1;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Unit.Value;
    }
    private static bool SameSlot(ClassSession a, ClassSession b) => a.Date == b.Date && a.StartTime == b.StartTime && a.EndTime == b.EndTime;
}

