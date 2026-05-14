using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Classroom.Dto;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Queries;

public record GetClassByIdQuery(Guid Id) : IRequest<ClassDto>;

public class GetClassByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetClassByIdQuery, ClassDto>
{
    public async Task<ClassDto> Handle(GetClassByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var result = await context.Classes
            .Where(c => c.Id == request.Id && c.TeacherId == userId)
            .Include(c => c.Subject)
            .Include(c => c.Students)
            .Include(c => c.Schedules)
            .Select(c => new ClassDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
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
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            throw new Exception("Class not found or you don't have permission to view it.");

        return result;
    }
}
