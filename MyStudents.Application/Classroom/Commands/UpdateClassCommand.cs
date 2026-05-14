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
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.TeacherId == userId, cancellationToken);

        if (entity == null)
            throw new Exception("Class not found or you don't have permission to update it.");

        // Check if subject exists
        var subject = await context.Subjects.AnyAsync(s => s.Id == command.SubjectId, cancellationToken);
        if (!subject) throw new Exception("Subject not found.");

        entity.Name = command.Name;
        entity.Code = command.Code;
        entity.CategoryOfClass = command.Category;
        entity.SubjectId = command.SubjectId;
        entity.StartDate = command.StartDate;
        entity.ExpectedEndDate = command.ExpectedEndDate;

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

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
