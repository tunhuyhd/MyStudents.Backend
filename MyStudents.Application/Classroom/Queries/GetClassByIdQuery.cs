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
            .Select(c => new ClassDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Category = c.CategoryOfClass,
                SubjectId = c.SubjectId,
                SubjectName = c.Subject.Name,
                StudentCount = c.Students.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            throw new Exception("Class not found or you don't have permission to view it.");

        return result;
    }
}
