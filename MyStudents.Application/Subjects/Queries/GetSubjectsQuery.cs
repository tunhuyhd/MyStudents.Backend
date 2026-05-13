using MediatR;
using MyStudents.Application.Common.Interfaces;
using MyStudents.Application.Subjects.Dto;
using Microsoft.EntityFrameworkCore;

namespace MyStudents.Application.Subjects.Queries;

public record GetSubjectsQuery() : IRequest<List<SubjectDto>>;

public class GetSubjectsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetSubjectsQuery, List<SubjectDto>>
{
    public async Task<List<SubjectDto>> Handle(GetSubjectsQuery request, CancellationToken cancellationToken)
    {
        return await context.Subjects
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            })
            .ToListAsync(cancellationToken);
    }
}
