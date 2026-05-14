using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Classroom.Dto;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Queries;

public record GetClassesQuery() : IRequest<List<ClassDto>>;

public class GetClassesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetClassesQuery, List<ClassDto>>
{
    public async Task<List<ClassDto>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        return await context.Classes
            .Where(c => c.TeacherId == userId)
            .Include(c => c.Subject)
            .Include(c => c.Students)
            .Include(c => c.Schedules)
            .Select(c => new ClassDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Status = c.Status,
                Category = c.CategoryOfClass,
                SubjectId = c.SubjectId,
                SubjectName = c.Subject.Name,
                StartDate = c.StartDate,
                ExpectedEndDate = c.ExpectedEndDate,
                StudentCount = c.Students.Count,
                Schedules = c.Schedules.Select(s => new ClassScheduleDto
                {
                    Id = s.Id,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    DurationHours = s.DurationHours
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}
