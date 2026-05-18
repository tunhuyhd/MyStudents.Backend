using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Classroom.Dto;
using Microsoft.EntityFrameworkCore;

using MyStudents.Application.Common.Models;

namespace MyStudents.Application.Classroom.Queries;

public record GetClassesQuery : IRequest<PaginatedList<ClassDto>>
{
    public string? SearchTerm { get; init; }
    public int? Year { get; init; }
    public string? SortBy { get; init; } // e.g., "name", "startDate", "studentCount"
    public bool SortDescending { get; init; } = true;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetClassesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetClassesQuery, PaginatedList<ClassDto>>
{
    public async Task<PaginatedList<ClassDto>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var query = context.Classes
            .Where(c => c.TeacherId == userId)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchTerm) || c.Code.ToLower().Contains(searchTerm));
        }

        // Filter by Year
        if (request.Year.HasValue)
        {
            var startOfYear = new DateOnly(request.Year.Value, 1, 1);
            var startOfNextYear = startOfYear.AddYears(1);
            query = query.Where(c => c.StartDate >= startOfYear && c.StartDate < startOfNextYear);
        }

        // Project
        var projectedQuery = query
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
            });

        // Sort
        projectedQuery = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDescending ? projectedQuery.OrderByDescending(c => c.Name) : projectedQuery.OrderBy(c => c.Name),
            "startdate" => request.SortDescending ? projectedQuery.OrderByDescending(c => c.StartDate) : projectedQuery.OrderBy(c => c.StartDate),
            "studentcount" => request.SortDescending ? projectedQuery.OrderByDescending(c => c.StudentCount) : projectedQuery.OrderBy(c => c.StudentCount),
            _ => request.SortDescending ? projectedQuery.OrderByDescending(c => c.StartDate) : projectedQuery.OrderBy(c => c.StartDate) // Default sort
        };

        return await PaginatedList<ClassDto>.CreateAsync(projectedQuery.AsNoTracking(), request.PageNumber, request.PageSize);
    }
}
