using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MyStudents.Domain.Entities.Enum;

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
