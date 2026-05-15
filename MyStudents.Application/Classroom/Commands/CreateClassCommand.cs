using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MyStudents.Domain.Entities.Enum;
using MyStudents.Application.Common.Exceptions;
using MyStudents.Application.Common.Extensions;

namespace MyStudents.Application.Classroom.Commands;

public record CreateClassScheduleInput(DayOfWeek DayOfWeek, TimeOnly StartTime, decimal DurationHours);

public record CreateClassCommand(
    string Name, 
    string Code, 
    CategoryOfClass Category, 
    Guid SubjectId,
    DateOnly StartDate,
    DateOnly ExpectedEndDate,
    List<CreateClassScheduleInput>? Schedules = null
) : IRequest<Guid>;

public class CreateClassCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<CreateClassCommand, Guid>
{
    public async Task<Guid> Handle(CreateClassCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        // Check if subject exists
        var subject = await context.Subjects.AnyAsync(s => s.Id == command.SubjectId, cancellationToken);
        if (!subject) throw new Exception("Subject not found.");

        // Overlap Check
        if (command.Schedules != null && command.Schedules.Any())
        {
            // 1. Check overlaps within the current request schedules
            var schedulesList = command.Schedules.ToList();
            for (int i = 0; i < schedulesList.Count; i++)
            {
                var s1 = schedulesList[i];
                var s1End = s1.StartTime.AddHours((double)s1.DurationHours);

                for (int j = i + 1; j < schedulesList.Count; j++)
                {
                    var s2 = schedulesList[j];
                    var s2End = s2.StartTime.AddHours((double)s2.DurationHours);

                    if (s1.DayOfWeek == s2.DayOfWeek && s1.StartTime < s2End && s2.StartTime < s1End)
                    {
                        throw new BusinessException("ERR_INTERNAL_SCHEDULE_OVERLAP", new Dictionary<string, string>
                        {
                            { "day", s1.DayOfWeek.ToString() },
                            { "time1", $"{s1.StartTime:HH:mm}-{s1End:HH:mm}" },
                            { "time2", $"{s2.StartTime:HH:mm}-{s2End:HH:mm}" }
                        });
                    }
                }
            }

            // 2. Check overlaps with other existing classes
            var teacherClasses = await context.Classes
                .Include(c => c.Schedules)
                .Where(c => c.TeacherId == userId && 
                           c.StartDate <= command.ExpectedEndDate && 
                           c.ExpectedEndDate >= command.StartDate)
                .ToListAsync(cancellationToken);

            foreach (var newSchedule in command.Schedules)
            {
                var newStart = newSchedule.StartTime;
                var newEnd = newStart.AddHours((double)newSchedule.DurationHours);

                foreach (var existingClass in teacherClasses)
                {
                    foreach (var existingSchedule in existingClass.Schedules)
                    {
                        if (existingSchedule.DayOfWeek == newSchedule.DayOfWeek)
                        {
                            var existingStart = existingSchedule.StartTime;
                            var existingEnd = existingStart.AddHours((double)existingSchedule.DurationHours);

                            if (newStart < existingEnd && existingStart < newEnd)
                            {
                                throw new BusinessException("ERR_SCHEDULE_OVERLAP", new Dictionary<string, string>
                                {
                                    { "className", existingClass.Name },
                                    { "day", newSchedule.DayOfWeek.ToString() },
                                    { "startTime", existingStart.ToString("HH:mm") },
                                    { "endTime", existingEnd.ToString("HH:mm") }
                                });
                            }
                        }
                    }
                }
            }
        }

        var entity = new Class
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Code = command.Code,
            CategoryOfClass = command.Category,
            SubjectId = command.SubjectId,
            TeacherId = userId,
            StartDate = command.StartDate,
            ExpectedEndDate = command.ExpectedEndDate
        };

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

            // Generate ClassSessions
            var currentDate = command.StartDate;
            int orderIndex = 1;
            while (currentDate <= command.ExpectedEndDate)
            {
                var matchingSchedules = command.Schedules.Where(s => s.DayOfWeek == currentDate.DayOfWeek);
                foreach (var s in matchingSchedules)
                {
                    entity.Sessions.Add(new ClassSession
                    {
                        Id = Guid.NewGuid(),
                        Date = currentDate,
                        StartTime = s.StartTime,
                        EndTime = s.StartTime.AddHours((double)s.DurationHours),
                        Status = SessionStatus.Scheduled,
                        OrderIndex = orderIndex++
                    });
                }
                currentDate = currentDate.AddDays(1);
            }
        }

        context.Classes.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
