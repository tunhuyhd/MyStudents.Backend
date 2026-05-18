using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Common.Models;
using MyStudents.Application.Classroom.Dto;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Queries;

public record GetClassStudentsQuery : IRequest<PaginatedList<StudentSummaryDto>>
{
    public Guid ClassId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetClassStudentsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetClassStudentsQuery, PaginatedList<StudentSummaryDto>>
{
    public async Task<PaginatedList<StudentSummaryDto>> Handle(GetClassStudentsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        // Verify class belongs to teacher
        var classExists = await context.Classes
            .AnyAsync(c => c.Id == request.ClassId && c.TeacherId == userId, cancellationToken);

        if (!classExists)
            throw new Exception("Class not found or you don't have permission.");

        var query = context.ClassStudents
            .Where(cs => cs.ClassId == request.ClassId)
            .Include(cs => cs.Student)
            .OrderBy(cs => cs.Student.LastName)
            .ThenBy(cs => cs.Student.FirstName)
            .Select(cs => new StudentSummaryDto
            {
                Id = cs.Student.Id,
                FullName = cs.Student.LastName + " " + cs.Student.FirstName,
                Email = cs.Student.Email,
                Status = cs.Status,
                JoinedAt = cs.JoinedAt
            });

        return await PaginatedList<StudentSummaryDto>.CreateAsync(query.AsNoTracking(), request.PageNumber, request.PageSize);
    }
}
