using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Students.Dto;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Classroom.Queries;

public record GetAvailableStudentsForClassQuery : IRequest<List<StudentDto>>
{
    public Guid ClassId { get; init; }
    public string? SearchTerm { get; init; }
}

public class GetAvailableStudentsForClassQueryHandler(IApplicationDbContext context) 
    : IRequestHandler<GetAvailableStudentsForClassQuery, List<StudentDto>>
{
    public async Task<List<StudentDto>> Handle(GetAvailableStudentsForClassQuery request, CancellationToken cancellationToken)
    {
        var query = context.Students
            .AsNoTracking()
            .Where(s => !context.ClassStudents.Any(cs => cs.ClassId == request.ClassId && cs.StudentId == s.Id));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(s => 
                s.FirstName.ToLower().Contains(term) || 
                s.LastName.ToLower().Contains(term) || 
                (s.Email != null && s.Email.ToLower().Contains(term)));
        }

        return await query
            .Take(20) // Limit for performance
            .Select(s => new StudentDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.Email,
                Phone = s.Phone,
            })
            .ToListAsync(cancellationToken);
    }
}
