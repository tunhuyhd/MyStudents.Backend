using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Classroom.Dto;
using Microsoft.EntityFrameworkCore;
using MyStudents.Domain.Entities.Enum;

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
                .ThenInclude(cs => cs.Student)
            .Include(c => c.Schedules)
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Attendances)
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
                }).ToList(),
                Students = c.Students.Select(cs => new StudentSummaryDto
                {
                    Id = cs.Student.Id,
                    FullName = cs.Student.LastName + " " + cs.Student.FirstName,
                    Email = cs.Student.Email,
                    Status = cs.Status
                }).ToList(),
                Sessions = c.Sessions.OrderBy(s => s.OrderIndex).Select(s => new SessionDto
                {
                    Id = s.Id,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Status = s.Status,
                    OrderIndex = s.OrderIndex,
                    Note = s.Note,
                    TotalCount = c.Students.Count,
                    PresentCount = s.Attendances.Count(a => a.Status == AttendanceStatus.Present)
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            throw new Exception("Class not found or you don't have permission to view it.");

        return result;
    }
}
