using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyStudents.Domain.Entities.Enum;
using MyStudents.Domain.Entities;

namespace MyStudents.Application.Classroom.Commands;

public record UpdateClassCommand(
    Guid Id, 
    string Name, 
    string Code, 
    CategoryOfClass Category, 
    Guid SubjectId,
    DateOnly StartDate,
    DateOnly ExpectedEndDate,
    ClassStatus Status,
    List<CreateClassScheduleInput>? Schedules = null
) : IRequest<Unit>;

public class UpdateClassCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateClassCommand, Unit>
{
    public async Task<Unit> Handle(UpdateClassCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var entity = await context.Classes
            .Include(c => c.Schedules)
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TeacherId == userId, cancellationToken);

        if (entity == null)
            throw new Exception("Class not found or you don't have permission to update it.");

        // Check if subject exists
        var subject = await context.Subjects.AnyAsync(s => s.Id == command.SubjectId, cancellationToken);
        if (!subject) throw new Exception("Subject not found.");

        var oldEndDate = entity.ExpectedEndDate;
        entity.Name = command.Name;
        entity.Code = command.Code;
        entity.CategoryOfClass = command.Category;
        entity.SubjectId = command.SubjectId;
        entity.StartDate = command.StartDate;
        entity.ExpectedEndDate = command.ExpectedEndDate;
        entity.Status = command.Status;

        // Simple sync for schedules: clear and re-add
        entity.Schedules.Clear();
        if (command.Schedules != null && command.Schedules.Any())
        {
            foreach (var s in command.Schedules)
            {
                entity.Schedules.Add(new ClassSchedule
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    DurationHours = s.DurationHours
                });
            }
        }

        // Sync Sessions if ExpectedEndDate changed
        if (command.ExpectedEndDate != oldEndDate)
        {
            if (command.ExpectedEndDate < oldEndDate)
            {
                // Remove sessions beyond the new end date
                var sessionsToRemove = entity.Sessions
                    .Where(s => s.Date > command.ExpectedEndDate)
                    .ToList();
                
                foreach (var s in sessionsToRemove)
                {
                    context.ClassSessions.Remove(s);
                }
            }
            else
            {
                // Add sessions between old and new end date
                var activeStudents = await context.ClassStudents
                    .Where(cs => cs.ClassId == entity.Id && cs.Status == StudentClassStatus.Active)
                    .Select(cs => cs.StudentId)
                    .ToListAsync(cancellationToken);

                int maxOrderIndex = entity.Sessions.Any() ? entity.Sessions.Max(s => s.OrderIndex) : 0;
                int nextOrderIndex = maxOrderIndex + 1;

                var currentDate = oldEndDate.AddDays(1);
                while (currentDate <= command.ExpectedEndDate)
                {
                    var matchingSchedules = command.Schedules?.Where(s => s.DayOfWeek == currentDate.DayOfWeek);
                    if (matchingSchedules != null)
                    {
                        foreach (var s in matchingSchedules)
                        {
                            var newSession = new ClassSession
                            {
                                Id = Guid.NewGuid(),
                                ClassId = entity.Id,
                                Date = currentDate,
                                StartTime = s.StartTime,
                                EndTime = s.StartTime.AddHours((double)s.DurationHours),
                                Status = SessionStatus.Scheduled,
                                OrderIndex = nextOrderIndex++
                            };
                            
                            context.ClassSessions.Add(newSession);
                            
                            // Add attendances for active students
                            foreach (var studentId in activeStudents)
                            {
                                context.Attendances.Add(new Attendance
                                {
                                    Session = newSession,
                                    StudentId = studentId,
                                    Status = AttendanceStatus.Present
                                });
                            }
                        }
                    }
                    currentDate = currentDate.AddDays(1);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
