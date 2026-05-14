using MediatR;
using MyStudents.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyStudents.Application.Students.Dto;

namespace MyStudents.Application.Students.Queries;

public record GetStudentsQuery : IRequest<List<StudentDto>>;

public class GetStudentsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetStudentsQuery, List<StudentDto>>
{
    public async Task<List<StudentDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

        return await context.Students
            .Where(s => s.CreatedBy == userId)
            .OrderByDescending(s => s.CreatedOn)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.Email,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                School = s.School,
                ParentName = s.ParentName,
                ParentPhone = s.ParentPhone,
                Phone = s.Phone,
                Address = s.Address,
                Note = s.Note,
                CreatedOn = s.CreatedOn
            })
            .ToListAsync(cancellationToken);
    }
}
